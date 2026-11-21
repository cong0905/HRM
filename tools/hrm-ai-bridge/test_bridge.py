import unittest
from bridge import parse_review
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
from unittest.mock import patch
import bridge

class ReviewTests(unittest.TestCase):
    def review(self, **kw):
        r = dict(verdict='ACCEPT', summary='done', findings=[], next_instructions='')
        r.update(kw)
        return json.dumps(r)

    def test_accept(self):
        self.assertEqual(parse_review(self.review())['verdict'], 'ACCEPT')

    def test_accept_with_findings_rejected(self):
        with self.assertRaises(ValueError):
            parse_review(self.review(findings=['broken']))

    def test_missing_instruction_rejected(self):
        with self.assertRaises(ValueError):
            parse_review(self.review(verdict='REVISE'))

    def test_unknown_verdict_rejected(self):
        with self.assertRaises(ValueError):
            parse_review(self.review(verdict='VERIFIED'))

    def test_blocked(self):
        self.assertEqual(parse_review(self.review(verdict='BLOCKED'))['verdict'], 'BLOCKED')

    def simulate(self, verdicts, rounds=3):
        # Mock provider execution, not a real model run. Keep artifacts local.
        with tempfile.TemporaryDirectory(dir=Path(__file__).parent) as td:
            root = Path(td)
            (root / 'CONTEXT.md').write_text('Test context', encoding='utf-8')
            (root / 'task.md').write_text('Test task', encoding='utf-8')
            replies = iter(verdicts)
            def provider(cli, cwd, prompt, prefix, stop_file, timeout):
                if cli[0] == 'codex':
                    answer = Path(cli[cli.index('--output-last-message') + 1])
                    if prefix.name == 'plan':
                        answer.write_text('Test instructions', encoding='utf-8')
                    else:
                        v = next(replies)
                        answer.write_text(self.review(verdict=v,
                            findings=['fix'] if v == 'REVISE' else [],
                            next_instructions='Fix the defect' if v == 'REVISE' else ''), encoding='utf-8')
                    return ''
                prefix.with_suffix('.stderr.log').write_text('', encoding='utf-8')
                return json.dumps({'status':'SUCCESS','response':'WORK_COMPLETE','conversation_id':'test'})
            with patch.object(bridge, 'HERE', root), patch.object(bridge, 'binaries', return_value=('agy','codex')), patch.object(bridge, 'command', side_effect=provider):
                code = bridge.run(SimpleNamespace(workspace=td, task=str(root/'task.md'), max_rounds=rounds, timeout=60))
            last = json.loads((root/'latest-run.json').read_text())
            state = json.loads((Path(last['run_dir'])/'state.json').read_text())
            self.assertFalse((root/'RUNNING.lock').exists())
            return code, state['status']

    def test_revision_loop_accepts(self):
        self.assertEqual(self.simulate(['REVISE','ACCEPT']), (0,'READY_FOR_USER_REVIEW'))

    def test_repeated_findings_stop(self):
        self.assertEqual(self.simulate(['REVISE','REVISE']), (2,'REPEATED_FINDINGS_NEEDS_USER'))

    def test_round_limit(self):
        self.assertEqual(self.simulate(['REVISE'], rounds=1), (2,'ROUND_LIMIT_NEEDS_USER'))

    def test_provider_blocked(self):
        self.assertEqual(self.simulate(['BLOCKED']), (2,'NEEDS_USER'))

if __name__ == '__main__':
    unittest.main()

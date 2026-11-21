#!/usr/bin/env python3
"""
Generate a clean, professional HRM Class Diagram for Draw.io
with color-coded modules, proper grid layout, and all relationships.
"""

# ============================================================
# DATA MODEL DEFINITION
# ============================================================

# Color scheme per module group
COLORS = {
    "recruit":   {"fill": "#dae8fc", "stroke": "#6c8ebf", "font": "#1a3a5c"},  # Blue
    "hr":        {"fill": "#d5e8d4", "stroke": "#82b366", "font": "#2d5016"},  # Green
    "time":      {"fill": "#fff2cc", "stroke": "#d6b656", "font": "#5c4a00"},  # Yellow/Gold
    "finance":   {"fill": "#f8cecc", "stroke": "#b85450", "font": "#6b1d1b"},  # Red/Pink
    "system":    {"fill": "#e1d5e7", "stroke": "#9673a6", "font": "#3d1f56"},  # Purple
    "policy":    {"fill": "#ffe6cc", "stroke": "#d79b00", "font": "#5c3d00"},  # Orange
}

# Table definitions: (id, name, color_key, fields)
# fields: list of (key_type, field_name)  where key_type = "PK", "FK", or ""
TABLES = [
    # ── Column 1 (Left): Tuyển dụng, Tài chính, Chính sách ──
    {
        "id": 200, "name": "Ứng viên\n(Candidate)", "color": "recruit",
        "x": 200, "y": -200,
        "fields": [
            ("PK", "<b><u>+ MaUngVien : int</u></b>"),
            ("FK", "+ MaTinTuyenDung : int"),
            ("",   "+ HoTen : string"),
            ("",   "+ Email : string?"),
            ("",   "+ SoDienThoai : string?"),
            ("",   "+ TrangThai : string"),
            ("",   "+ NgayNop : DateTime"),
        ]
    },
    {
        "id": 100, "name": "Tin tuyển dụng\n(Recruitment)", "color": "recruit",
        "x": 200, "y": 50,
        "fields": [
            ("PK", "<b><u>+ MaTinTuyenDung : int</u></b>"),
            ("",   "+ ViTriTuyenDung : string"),
            ("FK", "+ MaPhongBan : int?"),
            ("",   "+ SoLuongCanTuyen : int"),
            ("",   "+ MucLuongMin : decimal?"),
            ("",   "+ MucLuongMax : decimal?"),
            ("",   "+ ThoiHanNhanHoSo : DateTime?"),
            ("",   "+ TrangThai : string"),
        ]
    },
    {
        "id": 300, "name": "Phòng ban\n(Department)", "color": "hr",
        "x": 200, "y": 350,
        "fields": [
            ("PK", "<b><u>+ MaPhongBan : int</u></b>"),
            ("",   "+ TenPhongBan : string"),
            ("",   "+ MoTaChucNang : string?"),
            ("",   "+ NgayThanhLap : DateTime?"),
            ("",   "+ DiaDiemLamViec : string?"),
            ("",   "+ NganSach : decimal?"),
            ("",   "+ TrangThai : string"),
            ("FK", "+ MaTruongPhong : int?"),
        ]
    },
    {
        "id": 1100, "name": "Bảng lương\n(Payroll)", "color": "finance",
        "x": 200, "y": 650,
        "fields": [
            ("PK", "<b><u>+ MaBangLuong : int</u></b>"),
            ("FK", "+ MaNhanVien : int"),
            ("",   "+ Thang : int"),
            ("",   "+ Nam : int"),
            ("",   "+ LuongCoBan : decimal"),
            ("",   "+ TongPhuCap : decimal"),
            ("",   "+ SoNgayLamViec : int"),
            ("",   "+ TienLamThem : decimal"),
            ("",   "+ TongThuong : decimal"),
            ("",   "+ TongPhat : decimal"),
            ("",   "+ LuongThucNhan : decimal?"),
            ("",   "+ TrangThai : string"),
        ]
    },
    {
        "id": 1200, "name": "Chứng chỉ\n(Certificate)", "color": "policy",
        "x": 200, "y": 1050,
        "fields": [
            ("PK", "<b><u>+ MaChungChi : int</u></b>"),
            ("FK", "+ MaNhanVien : int"),
            ("",   "+ TenChungChi : string"),
            ("",   "+ LoaiChungChi : string?"),
            ("",   "+ ToChucCap : string?"),
            ("",   "+ NgayCap : DateTime"),
            ("",   "+ NgayHetHan : DateTime?"),
        ]
    },

    # ── Column 2 (Center): Tổ chức, Nhân sự, Hệ thống ──
    {
        "id": 400, "name": "Chức vụ\n(Position)", "color": "hr",
        "x": 600, "y": 50,
        "fields": [
            ("PK", "<b><u>+ MaChucVu : int</u></b>"),
            ("",   "+ TenChucVu : string"),
            ("",   "+ MoTa : string?"),
            ("",   "+ CapBac : int"),
        ]
    },
    {
        "id": 500, "name": "Nhân viên\n(Employee)", "color": "hr",
        "x": 600, "y": 250,
        "fields": [
            ("PK", "<b><u>+ MaNhanVien : int</u></b>"),
            ("",   "+ MaNV : string?"),
            ("",   "+ HoTen : string"),
            ("",   "+ NgaySinh : DateTime"),
            ("",   "+ GioiTinh : string?"),
            ("",   "+ CCCD : string?"),
            ("",   "+ DiaChi : string?"),
            ("",   "+ SoDienThoai : string?"),
            ("",   "+ Email : string?"),
            ("",   "+ TinhTrangHonNhan : string?"),
            ("FK", "+ MaPhongBan : int?"),
            ("FK", "+ MaChucVu : int?"),
            ("",   "+ NgayVaoLam : DateTime"),
            ("",   "+ MucLuong : decimal"),
            ("",   "+ TrangThai : string"),
        ]
    },
    {
        "id": 600, "name": "Tài khoản\n(Account)", "color": "system",
        "x": 600, "y": 750,
        "fields": [
            ("PK", "<b><u>+ MaTaiKhoan : int</u></b>"),
            ("FK", "+ MaNhanVien : int"),
            ("",   "+ TenDangNhap : string"),
            ("",   "+ MatKhauHash : string"),
            ("",   "+ VaiTro : string"),
            ("",   "+ TrangThai : string"),
            ("",   "+ LanDangNhapCuoi : DateTime?"),
        ]
    },
    {
        "id": 700, "name": "Báo cáo &amp; Thống kê\n(Dashboard)", "color": "system",
        "x": 600, "y": 1050,
        "fields": [
            ("",   "+ TongNhanVien : int"),
            ("",   "+ DiLamHomNay : int"),
            ("",   "+ NghiPhepHomNay : int"),
            ("",   "+ DiMuonHomNay : int"),
            ("",   "+ PhanTramTangNV : decimal"),
            ("",   "+ PhanTramDiLam : decimal"),
            ("",   "+ ChenhLechNghiPhep : int"),
            ("",   "+ ChenhLechDiMuon : int"),
        ]
    },

    # ── Column 3 (Right): Quản lý thời gian, Chính sách ──
    {
        "id": 800, "name": "Chấm công\n(Attendance)", "color": "time",
        "x": 1000, "y": 50,
        "fields": [
            ("PK", "<b><u>+ MaChamCong : int</u></b>"),
            ("FK", "+ MaNhanVien : int"),
            ("",   "+ NgayChamCong : DateTime"),
            ("",   "+ GioVao : TimeSpan?"),
            ("",   "+ GioRa : TimeSpan?"),
            ("",   "+ TongGioLam : decimal?"),
            ("",   "+ GioLamThem : decimal"),
            ("",   "+ TrangThai : string"),
        ]
    },
    {
        "id": 900, "name": "Đơn nghỉ phép\n(Leave Request)", "color": "time",
        "x": 1000, "y": 350,
        "fields": [
            ("PK", "<b><u>+ MaDonPhep : int</u></b>"),
            ("FK", "+ MaNhanVien : int"),
            ("FK", "+ MaLoaiPhep : int"),
            ("",   "+ NgayBatDau : DateTime"),
            ("",   "+ NgayKetThuc : DateTime"),
            ("",   "+ SoNgayNghi : decimal"),
            ("",   "+ LyDo : string"),
            ("",   "+ TrangThai : string"),
            ("FK", "+ NguoiPheDuyet : int?"),
        ]
    },
    {
        "id": 1000, "name": "Chương trình đào tạo\n(Training)", "color": "time",
        "x": 1000, "y": 650,
        "fields": [
            ("PK", "<b><u>+ MaDaoTao : int</u></b>"),
            ("",   "+ TenKhoaHoc : string"),
            ("",   "+ ThoiLuong : int?"),
            ("",   "+ GiangVien : string?"),
            ("",   "+ ChiPhi : decimal?"),
            ("",   "+ NgayBatDau : DateTime?"),
            ("",   "+ NgayKetThuc : DateTime?"),
            ("",   "+ TrangThai : string"),
        ]
    },
    {
        "id": 1300, "name": "Chính sách\n(Policy)", "color": "policy",
        "x": 1000, "y": 950,
        "fields": [
            ("PK", "<b><u>+ MaChinhSach : int</u></b>"),
            ("",   "+ TenChinhSach : string"),
            ("",   "+ LoaiChinhSach : string?"),
            ("",   "+ NoiDung : string"),
            ("",   "+ PhamViApDung : string?"),
            ("",   "+ NgayHieuLuc : DateTime"),
            ("FK", "+ NguoiPheDuyet : int?"),
            ("",   "+ TrangThai : string"),
        ]
    },
]

# Relationships: (source_table_id, target_table_id, src_card, tgt_card, exit, entry, style_extra)
# Cards: "1", "N", "0..1"
RELATIONSHIPS = [
    # UngVien N -> 1 TinTuyenDung (Col 1 to Col 1, top to bottom)
    (200, 100, "N", "1", "exitX=0.5;exitY=1;", "entryX=0.5;entryY=0;", ""),
    # TinTuyenDung N -> 1 PhongBan (Col 1 to Col 1, top to bottom)
    (100, 300, "N", "1", "exitX=0.5;exitY=1;", "entryX=0.5;entryY=0;", ""),
    
    # NhanVien N -> 1 PhongBan (Col 2 to Col 1, NhanVien exit left, PhongBan entry right)
    (500, 300, "N", "1", "exitX=0;exitY=0.25;", "entryX=1;entryY=0.5;", ""),
    
    # NhanVien N -> 1 ChucVu (Col 2 to Col 2, bottom to top)
    (500, 400, "N", "1", "exitX=0.5;exitY=0;", "entryX=0.5;entryY=1;", ""),
    
    # NhanVien 1 -> 1 TaiKhoan (Col 2 to Col 2, top to bottom)
    (500, 600, "1", "1", "exitX=0.5;exitY=1;", "entryX=0.5;entryY=0;", ""),
    
    # Dashboard --uses--> TaiKhoan (Col 2 to Col 2, bottom to top)
    (700, 600, "", "", "exitX=0.5;exitY=0;", "entryX=0.5;entryY=1;", "dashed=1;strokeColor=#999999;endArrow=open;endSize=6;"),
    
    # ChamCong N -> 1 NhanVien (Col 3 to Col 2, exit left, entry right)
    (800, 500, "N", "1", "exitX=0;exitY=0.5;", "entryX=1;entryY=0.15;", ""),
    
    # DonNghiPhep N -> 1 NhanVien (Col 3 to Col 2)
    (900, 500, "N", "1", "exitX=0;exitY=0.5;", "entryX=1;entryY=0.4;", ""),
    
    # DaoTao N -> 1 NhanVien (Col 3 to Col 2)
    (1000, 500, "N", "1", "exitX=0;exitY=0.5;", "entryX=1;entryY=0.65;", ""),
    
    # ChinhSach N -> 1 NhanVien (Col 3 to Col 2)
    (1300, 500, "N", "1", "exitX=0;exitY=0.5;", "entryX=1;entryY=0.9;", ""),
    
    # BangLuong N -> 1 NhanVien (Col 1 to Col 2, exit right, entry left)
    (1100, 500, "N", "1", "exitX=1;exitY=0.5;", "entryX=0;entryY=0.65;", ""),
    
    # ChungChi N -> 1 NhanVien (Col 1 to Col 2, exit right, entry left)
    (1200, 500, "N", "1", "exitX=1;exitY=0.5;", "entryX=0;entryY=0.9;", ""),
]

# ============================================================
# XML GENERATION
# ============================================================

TABLE_WIDTH = 260
ROW_HEIGHT = 26
HEADER_HEIGHT = 30
PK_COL_WIDTH = 40
FIELD_COL_WIDTH = TABLE_WIDTH - PK_COL_WIDTH

def xml_escape(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;").replace('"', "&quot;")

def gen_table_xml(table, id_counter):
    """Generate XML for a single table (swimlane with rows)."""
    lines = []
    tid = table["id"]
    color = COLORS[table["color"]]
    fields = table["fields"]
    num_fields = len(fields)
    table_height = HEADER_HEIGHT + num_fields * ROW_HEIGHT
    
    # Swimlane header
    style = (
        f"swimlane;fontStyle=1;childLayout=stackLayout;horizontal=1;"
        f"startSize={HEADER_HEIGHT};horizontalStack=0;resizeParent=1;"
        f"resizeParentMax=0;resizeLast=0;collapsible=1;marginBottom=0;"
        f"html=1;whiteSpace=wrap;"
        f"fillColor={color['fill']};strokeColor={color['stroke']};"
        f"fontColor={color['font']};fontSize=12;align=center;"
        f"rounded=1;arcSize=8;shadow=1;"
    )
    name_escaped = table["name"].replace("&", "&amp;").replace("\n", "&lt;br&gt;")
    lines.append(
        f'        <mxCell id="{tid}" parent="1" '
        f'style="{style}" '
        f'value="{name_escaped}" vertex="1">'
    )
    lines.append(
        f'          <mxGeometry x="{table["x"]}" y="{table["y"]}" '
        f'width="{TABLE_WIDTH}" height="{table_height}" as="geometry" />'
    )
    lines.append(f'        </mxCell>')
    
    # Fields
    for i, (key_type, field_name) in enumerate(fields):
        row_id = tid + 1 + i * 3
        left_id = row_id + 1
        right_id = row_id + 2
        y_offset = HEADER_HEIGHT + i * ROW_HEIGHT
        
        # Row container
        row_style = (
            "shape=tableRow;horizontal=0;startSize=0;swimlaneHead=0;swimlaneBody=0;"
            "top=0;left=0;bottom=0;right=0;collapsible=0;dropTarget=0;"
            "fillColor=none;points=[[0,0.5],[1,0.5]];portConstraint=eastwest;html=1;"
        )
        lines.append(
            f'        <mxCell id="{row_id}" parent="{tid}" '
            f'style="{row_style}" value="" vertex="1">'
        )
        lines.append(
            f'          <mxGeometry y="{y_offset}" width="{TABLE_WIDTH}" '
            f'height="{ROW_HEIGHT}" as="geometry" />'
        )
        lines.append(f'        </mxCell>')
        
        # Left cell (PK/FK marker)
        left_style = (
            "shape=partialRectangle;connectable=0;fillColor=none;"
            "top=0;left=0;bottom=0;right=0;"
            "align=center;verticalAlign=middle;"
            "spacingLeft=4;spacingRight=4;overflow=hidden;whiteSpace=wrap;html=1;"
            f"strokeColor={color['stroke']};fontColor={color['font']};"
            "fontSize=10;fontStyle=1;"
        )
        lines.append(
            f'        <mxCell id="{left_id}" parent="{row_id}" '
            f'style="{left_style}" value="{key_type}" vertex="1">'
        )
        lines.append(
            f'          <mxGeometry width="{PK_COL_WIDTH}" '
            f'height="{ROW_HEIGHT}" as="geometry" />'
        )
        lines.append(f'        </mxCell>')
        
        # Right cell (field name)
        right_style = (
            "shape=partialRectangle;connectable=0;fillColor=none;"
            "top=0;left=0;bottom=0;right=0;"
            "align=left;verticalAlign=middle;"
            "spacingLeft=6;spacingRight=6;overflow=hidden;whiteSpace=wrap;html=1;"
            f"strokeColor={color['stroke']};fontColor={color['font']};"
            "fontSize=11;"
        )
        # Escape field name for XML attributes
        field_escaped = (field_name
            .replace('&', '&amp;')
            .replace('<', '&lt;')
            .replace('>', '&gt;')
            .replace('"', '&quot;'))
        lines.append(
            f'        <mxCell id="{right_id}" parent="{row_id}" '
            f'style="{right_style}" value="{field_escaped}" vertex="1">'
        )
        lines.append(
            f'          <mxGeometry x="{PK_COL_WIDTH}" width="{FIELD_COL_WIDTH}" '
            f'height="{ROW_HEIGHT}" as="geometry" />'
        )
        lines.append(f'        </mxCell>')
    
    return lines

def gen_edge_xml(edge_id, src_id, tgt_id, src_card, tgt_card, exit_pt, entry_pt, extra_style):
    """Generate XML for an edge (relationship)."""
    lines = []
    base_style = (
        "edgeStyle=orthogonalEdgeStyle;rounded=1;orthogonalLoop=1;"
        "jettySize=auto;html=1;strokeWidth=1.5;"
    )
    
    if extra_style:
        base_style += extra_style
    else:
        base_style += "strokeColor=#666666;endArrow=none;startArrow=none;"
    
    if exit_pt:
        base_style += exit_pt
    if entry_pt:
        base_style += entry_pt
    
    lines.append(
        f'        <mxCell id="{edge_id}" parent="1" source="{src_id}" target="{tgt_id}" '
        f'edge="1" style="{base_style}">'
    )
    lines.append(f'          <mxGeometry relative="1" as="geometry" />')
    lines.append(f'        </mxCell>')
    
    # Source cardinality label
    if src_card:
        label_id = edge_id + 1
        lines.append(
            f'        <mxCell id="{label_id}" parent="{edge_id}" value="{src_card}" '
            f'vertex="1" connectable="0" '
            f'style="edgeLabel;resizable=0;align=center;verticalAlign=middle;'
            f'html=1;fontColor=#333333;fontSize=12;fontStyle=1;">'
        )
        lines.append(
            f'          <mxGeometry x="-0.85" relative="1" as="geometry">'
        )
        lines.append(f'            <mxPoint x="12" y="-12" as="offset" />')
        lines.append(f'          </mxGeometry>')
        lines.append(f'        </mxCell>')
    
    # Target cardinality label
    if tgt_card:
        label_id = edge_id + 2
        lines.append(
            f'        <mxCell id="{label_id}" parent="{edge_id}" value="{tgt_card}" '
            f'vertex="1" connectable="0" '
            f'style="edgeLabel;resizable=0;align=center;verticalAlign=middle;'
            f'html=1;fontColor=#333333;fontSize=12;fontStyle=1;">'
        )
        lines.append(
            f'          <mxGeometry x="0.85" relative="1" as="geometry">'
        )
        lines.append(f'            <mxPoint x="-12" y="-12" as="offset" />')
        lines.append(f'          </mxGeometry>')
        lines.append(f'        </mxCell>')
    
    return lines

def gen_title_xml():
    """Generate a title label for the diagram."""
    lines = []
    lines.append(
        '        <mxCell id="10" parent="1" vertex="1" '
        'style="text;html=1;align=center;verticalAlign=middle;resizable=0;'
        'points=[];autosize=1;strokeColor=none;fillColor=none;'
        'fontColor=#1a1a2e;fontSize=20;fontStyle=1;fontFamily=Segoe UI;" '
        'value="HRM System - Class Diagram">'
    )
    lines.append(
        '          <mxGeometry x="600" y="-300" width="400" height="40" as="geometry" />'
    )
    lines.append('        </mxCell>')
    
    # Legend
    legend_items = [
        ("recruit", "Tuyển dụng", 240),
        ("hr", "Nhân sự", 400),
        ("time", "Thời gian", 560),
        ("finance", "Tài chính", 720),
        ("system", "Hệ thống", 880),
        ("policy", "Chính sách", 1040),
    ]
    for i, (key, label, x) in enumerate(legend_items):
        c = COLORS[key]
        lid = 20 + i
        lines.append(
            f'        <mxCell id="{lid}" parent="1" vertex="1" '
            f'style="rounded=1;whiteSpace=wrap;html=1;fillColor={c["fill"]};'
            f'strokeColor={c["stroke"]};fontColor={c["font"]};fontSize=10;'
            f'fontStyle=1;arcSize=40;shadow=0;" '
            f'value="{label}">'
        )
        lines.append(
            f'          <mxGeometry x="{x}" y="-260" width="120" height="24" as="geometry" />'
        )
        lines.append(f'        </mxCell>')
    
    return lines

def generate_full_diagram():
    """Generate the complete .drawio XML."""
    lines = []
    lines.append('<?xml version="1.0" encoding="UTF-8"?>')
    lines.append('<mxfile host="app.diagrams.net">')
    lines.append('  <diagram id="hrm-class-diagram" name="HRM Class Diagram">')
    lines.append(
        '    <mxGraphModel dx="2400" dy="1600" grid="1" gridSize="10" '
        'guides="1" tooltips="1" connect="1" arrows="1" fold="1" '
        'page="1" pageScale="1" pageWidth="1600" pageHeight="1400" '
        'math="0" shadow="0">'
    )
    lines.append('      <root>')
    lines.append('        <mxCell id="0" />')
    lines.append('        <mxCell id="1" parent="0" />')
    
    # Title & Legend
    lines.extend(gen_title_xml())
    
    # Tables
    for table in TABLES:
        lines.extend(gen_table_xml(table, 0))
    
    # Edges
    edge_base_id = 2000
    for i, (src, tgt, sc, tc, ex, en, extra) in enumerate(RELATIONSHIPS):
        eid = edge_base_id + i * 10
        lines.extend(gen_edge_xml(eid, src, tgt, sc, tc, ex, en, extra))
    
    lines.append('      </root>')
    lines.append('    </mxGraphModel>')
    lines.append('  </diagram>')
    lines.append('</mxfile>')
    
    return '\n'.join(lines)

# ============================================================
# MAIN
# ============================================================
if __name__ == "__main__":
    import sys
    sys.stdout.reconfigure(encoding='utf-8')
    
    output = generate_full_diagram()
    output_path = r"d:\dotnet project\db\docs\HRM_Class_Diagram.drawio"
    
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write(output)
    
    print(f"Generated diagram with:")
    print(f"  - {len(TABLES)} tables")
    print(f"  - {len(RELATIONSHIPS)} relationships")
    print(f"  - Color-coded by module")
    print(f"  - File: {output_path}")
    print("Done!")

#!/usr/bin/env python3
"""
Generate Draw.io ERD Diagram for 5 Core HRM Entities.
Outputs: docs/HRM_5_Core_Entities_ERD.drawio
"""
import os

COLORS = {
    "recruit":   {"fill": "#dae8fc", "stroke": "#6c8ebf", "font": "#1a3a5c"},  # Blue
    "hr":        {"fill": "#d5e8d4", "stroke": "#82b366", "font": "#2d5016"},  # Green
    "time":      {"fill": "#fff2cc", "stroke": "#d6b656", "font": "#5c4a00"},  # Yellow
    "finance":   {"fill": "#f8cecc", "stroke": "#b85450", "font": "#6b1d1b"},  # Red
}

TABLES = [
    {
        "id": 100, "name": "Phòng ban\n(Department)", "color": "hr",
        "x": 100, "y": 100,
        "fields": [
            ("PK", "<b><u>+ MaPhongBan : int</u></b>"),
            ("",   "+ TenPhongBan : string"),
            ("",   "+ MoTaChucNang : string?"),
            ("",   "+ DiaDiemLamViec : string?"),
            ("",   "+ NganSach : decimal?"),
            ("",   "+ TrangThai : string"),
            ("FK", "+ MaTruongPhong : int?"),
        ]
    },
    {
        "id": 200, "name": "Nhân viên\n(Employee)", "color": "hr",
        "x": 550, "y": 100,
        "fields": [
            ("PK", "<b><u>+ MaNhanVien : int</u></b>"),
            ("",   "+ MaNV : string?"),
            ("",   "+ HoTen : string"),
            ("",   "+ NgaySinh : DateTime"),
            ("",   "+ CCCD : string?"),
            ("",   "+ Email : string?"),
            ("FK", "+ MaPhongBan : int?"),
            ("",   "+ MucLuong : decimal"),
            ("",   "+ NgayVaoLam : DateTime"),
            ("",   "+ TrangThai : string"),
        ]
    },
    {
        "id": 300, "name": "Chấm công\n(Attendance)", "color": "time",
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
        "id": 400, "name": "Bảng lương\n(Payroll)", "color": "finance",
        "x": 1000, "y": 380,
        "fields": [
            ("PK", "<b><u>+ MaBangLuong : int</u></b>"),
            ("FK", "+ MaNhanVien : int"),
            ("",   "+ Thang : int"),
            ("",   "+ Nam : int"),
            ("",   "+ LuongCoBan : decimal"),
            ("",   "+ TongPhuCap : decimal"),
            ("",   "+ TongThuong : decimal"),
            ("",   "+ TongPhat : decimal"),
            ("",   "+ LuongThucNhan : decimal?"),
            ("",   "+ TrangThai : string"),
        ]
    },
    {
        "id": 500, "name": "Tin tuyển dụng\n(Job Posting)", "color": "recruit",
        "x": 100, "y": 420,
        "fields": [
            ("PK", "<b><u>+ MaTinTuyenDung : int</u></b>"),
            ("",   "+ ViTriTuyenDung : string"),
            ("FK", "+ MaPhongBan : int?"),
            ("FK", "+ NguoiTao : int?"),
            ("",   "+ SoLuongCanTuyen : int"),
            ("",   "+ MucLuongMin : decimal?"),
            ("",   "+ MucLuongMax : decimal?"),
            ("",   "+ ThoiHanNhanHoSo : DateTime?"),
            ("",   "+ TrangThai : string"),
        ]
    },
    {
        "id": 600, "name": "Ứng viên\n(Candidate)", "color": "recruit",
        "x": 100, "y": 760,
        "fields": [
            ("PK", "<b><u>+ MaUngVien : int</u></b>"),
            ("FK", "+ MaTinTuyenDung : int"),
            ("",   "+ HoTen : string"),
            ("",   "+ Email : string?"),
            ("",   "+ SoDienThoai : string?"),
            ("",   "+ TrangThai : string"),
        ]
    },
]

RELATIONSHIPS = [
    # NhanVien N -> 1 PhongBan (Thuoc phong ban)
    (200, 100, "N", "1", "exitX=0;exitY=0.25;", "entryX=1;entryY=0.25;", ""),
    # PhongBan 1 -> 1 NhanVien (Truong phong)
    (100, 200, "1", "0..1", "exitX=1;exitY=0.75;", "entryX=0;entryY=0.75;", "dashed=1;strokeColor=#2d5016;"),
    # ChamCong N -> 1 NhanVien
    (300, 200, "N", "1", "exitX=0;exitY=0.5;", "entryX=1;entryY=0.25;", ""),
    # BangLuong N -> 1 NhanVien
    (400, 200, "N", "1", "exitX=0;exitY=0.5;", "entryX=1;entryY=0.75;", ""),
    # TinTuyenDung N -> 1 PhongBan
    (500, 100, "N", "1", "exitX=0.5;exitY=0;", "entryX=0.5;entryY=1;", ""),
    # TinTuyenDung N -> 1 NhanVien (Nguoi tao)
    (500, 200, "N", "1", "exitX=1;exitY=0.25;", "entryX=0.25;entryY=1;", "dashed=1;"),
    # UngVien N -> 1 TinTuyenDung
    (600, 500, "N", "1", "exitX=0.5;exitY=0;", "entryX=0.5;entryY=1;", ""),
]

TABLE_WIDTH = 270
ROW_HEIGHT = 26
HEADER_HEIGHT = 32
PK_COL_WIDTH = 40
FIELD_COL_WIDTH = TABLE_WIDTH - PK_COL_WIDTH

def gen_table_xml(table):
    lines = []
    tid = table["id"]
    color = COLORS[table["color"]]
    fields = table["fields"]
    num_fields = len(fields)
    table_height = HEADER_HEIGHT + num_fields * ROW_HEIGHT
    
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
    lines.append(f'        <mxCell id="{tid}" parent="1" style="{style}" value="{name_escaped}" vertex="1">')
    lines.append(f'          <mxGeometry x="{table["x"]}" y="{table["y"]}" width="{TABLE_WIDTH}" height="{table_height}" as="geometry" />')
    lines.append(f'        </mxCell>')
    
    for i, (key_type, field_name) in enumerate(fields):
        row_id = tid + 1 + i * 3
        left_id = row_id + 1
        right_id = row_id + 2
        y_offset = HEADER_HEIGHT + i * ROW_HEIGHT
        
        row_style = (
            "shape=tableRow;horizontal=0;startSize=0;swimlaneHead=0;swimlaneBody=0;"
            "top=0;left=0;bottom=0;right=0;collapsible=0;dropTarget=0;"
            "fillColor=none;points=[[0,0.5],[1,0.5]];portConstraint=eastwest;html=1;"
        )
        lines.append(f'        <mxCell id="{row_id}" parent="{tid}" style="{row_style}" value="" vertex="1">')
        lines.append(f'          <mxGeometry y="{y_offset}" width="{TABLE_WIDTH}" height="{ROW_HEIGHT}" as="geometry" />')
        lines.append(f'        </mxCell>')
        
        left_style = (
            "shape=partialRectangle;connectable=0;fillColor=none;"
            "top=0;left=0;bottom=0;right=0;align=center;verticalAlign=middle;"
            "spacingLeft=4;spacingRight=4;overflow=hidden;whiteSpace=wrap;html=1;"
            f"strokeColor={color['stroke']};fontColor={color['font']};fontSize=10;fontStyle=1;"
        )
        lines.append(f'        <mxCell id="{left_id}" parent="{row_id}" style="{left_style}" value="{key_type}" vertex="1">')
        lines.append(f'          <mxGeometry width="{PK_COL_WIDTH}" height="{ROW_HEIGHT}" as="geometry" />')
        lines.append(f'        </mxCell>')
        
        right_style = (
            "shape=partialRectangle;connectable=0;fillColor=none;"
            "top=0;left=0;bottom=0;right=0;align=left;verticalAlign=middle;"
            "spacingLeft=6;spacingRight=6;overflow=hidden;whiteSpace=wrap;html=1;"
            f"strokeColor={color['stroke']};fontColor={color['font']};fontSize=11;"
        )
        field_escaped = field_name.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;').replace('"', '&quot;')
        lines.append(f'        <mxCell id="{right_id}" parent="{row_id}" style="{right_style}" value="{field_escaped}" vertex="1">')
        lines.append(f'          <mxGeometry x="{PK_COL_WIDTH}" width="{FIELD_COL_WIDTH}" height="{ROW_HEIGHT}" as="geometry" />')
        lines.append(f'        </mxCell>')
    
    return lines

def gen_edge_xml(edge_id, src_id, tgt_id, src_card, tgt_card, exit_pt, entry_pt, extra_style):
    lines = []
    base_style = "edgeStyle=orthogonalEdgeStyle;rounded=1;orthogonalLoop=1;jettySize=auto;html=1;strokeWidth=1.5;"
    if extra_style:
        base_style += extra_style
    else:
        base_style += "strokeColor=#555555;endArrow=none;startArrow=none;"
    if exit_pt:
        base_style += exit_pt
    if entry_pt:
        base_style += entry_pt
    
    lines.append(f'        <mxCell id="{edge_id}" parent="1" source="{src_id}" target="{tgt_id}" edge="1" style="{base_style}">')
    lines.append(f'          <mxGeometry relative="1" as="geometry" />')
    lines.append(f'        </mxCell>')
    
    if src_card:
        label_id = edge_id + 1
        lines.append(f'        <mxCell id="{label_id}" parent="{edge_id}" value="{src_card}" vertex="1" connectable="0" style="edgeLabel;resizable=0;align=center;verticalAlign=middle;html=1;fontColor=#111111;fontSize=12;fontStyle=1;">')
        lines.append(f'          <mxGeometry x="-0.8" relative="1" as="geometry"><mxPoint x="10" y="-12" as="offset" /></mxGeometry>')
        lines.append(f'        </mxCell>')
    
    if tgt_card:
        label_id = edge_id + 2
        lines.append(f'        <mxCell id="{label_id}" parent="{edge_id}" value="{tgt_card}" vertex="1" connectable="0" style="edgeLabel;resizable=0;align=center;verticalAlign=middle;html=1;fontColor=#111111;fontSize=12;fontStyle=1;">')
        lines.append(f'          <mxGeometry x="0.8" relative="1" as="geometry"><mxPoint x="-10" y="-12" as="offset" /></mxGeometry>')
        lines.append(f'        </mxCell>')
    
    return lines

def main():
    lines = []
    lines.append('<?xml version="1.0" encoding="UTF-8"?>')
    lines.append('<mxfile host="app.diagrams.net">')
    lines.append('  <diagram id="hrm-5-entities" name="Sơ đồ ERD 5 Thực thể Chính HRM">')
    lines.append('    <mxGraphModel dx="1600" dy="1200" grid="1" gridSize="10" guides="1" tooltips="1" connect="1" arrows="1" fold="1" page="1" pageScale="1" pageWidth="1400" pageHeight="1100" math="0" shadow="0">')
    lines.append('      <root>')
    lines.append('        <mxCell id="0" />')
    lines.append('        <mxCell id="1" parent="0" />')
    
    # Title
    lines.append('        <mxCell id="10" parent="1" vertex="1" style="text;html=1;align=center;verticalAlign=middle;resizable=0;points=[];autosize=1;strokeColor=none;fillColor=none;fontColor=#1a1a2e;fontSize=22;fontStyle=1;fontFamily=Segoe UI;" value="SƠ ĐỒ THỰC THỂ QUAN HỆ (ERD) - 5 THỰC THỂ CHÍNH HRM">')
    lines.append('          <mxGeometry x="350" y="-20" width="700" height="40" as="geometry" />')
    lines.append('        </mxCell>')
    
    for table in TABLES:
        lines.extend(gen_table_xml(table))
    
    for i, (src, tgt, sc, tc, ex, en, extra) in enumerate(RELATIONSHIPS):
        eid = 2000 + i * 10
        lines.extend(gen_edge_xml(eid, src, tgt, sc, tc, ex, en, extra))
    
    lines.append('      </root>')
    lines.append('    </mxGraphModel>')
    lines.append('  </diagram>')
    lines.append('</mxfile>')
    
    out_dir = os.path.join(os.path.dirname(__file__), "docs")
    os.makedirs(out_dir, exist_ok=True)
    out_file = os.path.join(out_dir, "HRM_ERD_5_Entities.drawio")
    
    with open(out_file, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))
    print(f"Generated successfully: {out_file}")

if __name__ == "__main__":
    main()

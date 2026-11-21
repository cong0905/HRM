#!/usr/bin/env python3
"""
Generate Draw.io Chen Notation ERD Diagram for 5 Core HRM Entities, including Foreign Keys (FK).
Outputs: docs/HRM_ERD_5_Entities.drawio
"""
import os

def create_node(id, val, style, x, y, w, h):
    val_esc = val.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;").replace('"', "&quot;")
    return (
        f'        <mxCell id="{id}" parent="1" vertex="1" style="{style}" value="{val_esc}">\n'
        f'          <mxGeometry x="{x}" y="{y}" width="{w}" height="{h}" as="geometry" />\n'
        f'        </mxCell>'
    )

def create_edge(id, src, tgt, val="", extra_style=""):
    style = (
        "edgeStyle=straightEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;"
        "strokeWidth=2;strokeColor=#555555;endArrow=none;startArrow=none;"
        "fontColor=#000000;labelBackgroundColor=#ffffff;fontSize=14;fontStyle=1;"
    )
    if extra_style:
        style += extra_style
    
    val_esc = val.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;").replace('"', "&quot;")
    return (
        f'        <mxCell id="{id}" parent="1" source="{src}" target="{tgt}" edge="1" value="{val_esc}" style="{style}">\n'
        f'          <mxGeometry relative="1" as="geometry" />\n'
        f'        </mxCell>'
    )

def main():
    nodes = []
    edges = []
    
    entity_style = "rounded=0;whiteSpace=wrap;html=1;fillColor=#ffffff;strokeColor=#000000;strokeWidth=2;fontStyle=1;fontSize=13;fontFamily=Segoe UI;fontColor=#000000;"
    rel_style = "rhombus;whiteSpace=wrap;html=1;fillColor=#ffffff;strokeColor=#000000;strokeWidth=1.5;fontSize=12;fontFamily=Segoe UI;fontColor=#000000;"
    attr_style = "ellipse;whiteSpace=wrap;html=1;fillColor=#ffffff;strokeColor=#000000;strokeWidth=1;fontSize=11;fontFamily=Segoe UI;fontColor=#000000;"
    fk_attr_style = "ellipse;whiteSpace=wrap;html=1;fillColor=#ffffff;strokeColor=#000000;strokeWidth=1;dashed=1;fontSize=11;fontFamily=Segoe UI;fontColor=#000000;"
    
    # Title
    nodes.append(create_node(10, "SƠ ĐỒ THỰC THỂ QUAN HỆ (CHEN ERD CÓ KHÓA NGOẠI) - 5 THỰC THỂ HRM", "text;html=1;align=center;verticalAlign=middle;resizable=0;points=[];autosize=1;strokeColor=none;fillColor=none;fontColor=#ffffff;fontSize=20;fontStyle=1;fontFamily=Segoe UI;", 300, 20, 800, 40))

    # ============================================================
    # ENTITIES (Hình chữ nhật)
    # ============================================================
    # 1. PHÒNG BAN (100)
    nodes.append(create_node(100, "PHÒNG BAN", entity_style, 250, 450, 150, 60))
    # 2. NHÂN VIÊN (200)
    nodes.append(create_node(200, "NHÂN VIÊN", entity_style, 700, 450, 150, 60))
    # 3. CHẤM CÔNG (300)
    nodes.append(create_node(300, "CHẤM CÔNG", entity_style, 1150, 200, 150, 60))
    # 4. BẢNG LƯƠNG (400)
    nodes.append(create_node(400, "BẢNG LƯƠNG", entity_style, 1150, 700, 150, 60))
    # 5. TIN TUYỂN DỤNG (500)
    nodes.append(create_node(500, "TIN TUYỂN DỤNG", entity_style, 250, 850, 150, 60))

    # ============================================================
    # RELATIONSHIPS (Hình hình thoi)
    # ============================================================
    # Mối quan hệ "Thuộc về": PHÒNG BAN (1) — Thuộc về — (N) NHÂN VIÊN
    nodes.append(create_node(1000, "Thuộc về", rel_style, 480, 390, 110, 50))
    edges.append(create_edge(2001, 100, 1000, val="1"))
    edges.append(create_edge(2002, 1000, 200, val="N"))

    # Mối quan hệ "Quản lý": PHÒNG BAN (1) — Quản lý — (1) NHÂN VIÊN
    nodes.append(create_node(1010, "Quản lý", rel_style, 480, 520, 110, 50))
    edges.append(create_edge(2003, 100, 1010, val="1"))
    edges.append(create_edge(2004, 1010, 200, val="1"))

    # Mối quan hệ "Có" (Chấm công): NHÂN VIÊN (1) — Có — (N) CHẤM CÔNG
    nodes.append(create_node(1020, "Có", rel_style, 950, 300, 110, 50))
    edges.append(create_edge(2005, 200, 1020, val="1"))
    edges.append(create_edge(2006, 1020, 300, val="N"))

    # Mối quan hệ "Có" (Bảng lương): NHÂN VIÊN (1) — Có — (N) BẢNG LƯƠNG
    nodes.append(create_node(1030, "Có", rel_style, 950, 600, 110, 50))
    edges.append(create_edge(2007, 200, 1030, val="1"))
    edges.append(create_edge(2008, 1030, 400, val="N"))

    # Mối quan hệ "Đăng tin": PHÒNG BAN (1) — Đăng tin — (N) TIN TUYỂN DỤNG
    nodes.append(create_node(1040, "Đăng tin", rel_style, 270, 650, 110, 50))
    edges.append(create_edge(2009, 100, 1040, val="1"))
    edges.append(create_edge(2010, 1040, 500, val="N"))

    # ============================================================
    # ATTRIBUTES (Hình elip) & NỐI THỰC THỂ -> THUỘC TÍNH
    # ============================================================
    attr_edge_style = "strokeWidth=1;strokeColor=#888888;"
    
    # --- Attributes of PHÒNG BAN (100) ---
    pb_attrs = [
        (110, "<u>MaPhongBan</u>", 100, 340, False),
        (111, "TenPhongBan", 260, 340, False),
        (112, "DiaDiemLamViec", 60, 420, False),
        (113, "NganSach", 70, 510, False),
        (114, "TrangThai", 200, 550, False),
        (115, "MaTruongPhong (FK)", 330, 550, True),  # FK
    ]
    for aid, name, ax, ay, is_fk in pb_attrs:
        s = fk_attr_style if is_fk else attr_style
        nodes.append(create_node(aid, name, s, ax, ay, 130 if is_fk else 120, 45))
        edges.append(create_edge(3000 + aid, 100, aid, extra_style=attr_edge_style))

    # --- Attributes of NHÂN VIÊN (200) ---
    nv_attrs = [
        (210, "<u>MaNhanVien</u>", 540, 310, False),
        (211, "MaNV", 690, 300, False),
        (212, "HoTen", 820, 310, False),
        (213, "CCCD", 890, 400, False),
        (214, "Email", 890, 470, False),
        (215, "MucLuong", 860, 550, False),
        (216, "NgayVaoLam", 730, 590, False),
        (217, "TrangThai", 590, 590, False),
        (218, "MaPhongBan (FK)", 450, 560, True),  # FK
    ]
    for aid, name, ax, ay, is_fk in nv_attrs:
        s = fk_attr_style if is_fk else attr_style
        nodes.append(create_node(aid, name, s, ax, ay, 130 if is_fk else 120, 45))
        edges.append(create_edge(3000 + aid, 200, aid, extra_style=attr_edge_style))

    # --- Attributes of CHẤM CÔNG (300) ---
    cc_attrs = [
        (310, "<u>MaChamCong</u>", 1165, 70, False),
        (311, "MaNhanVien (FK)", 1020, 100, True),  # FK
        (312, "NgayChamCong", 1300, 120, False),
        (313, "GioVao", 1330, 200, False),
        (314, "GioRa", 1300, 280, False),
        (315, "TongGioLam", 1165, 310, False),
    ]
    for aid, name, ax, ay, is_fk in cc_attrs:
        s = fk_attr_style if is_fk else attr_style
        nodes.append(create_node(aid, name, s, ax, ay, 130 if is_fk else 120, 45))
        edges.append(create_edge(3000 + aid, 300, aid, extra_style=attr_edge_style))

    # --- Attributes of BẢNG LƯƠNG (400) ---
    bl_attrs = [
        (410, "<u>MaBangLuong</u>", 1165, 570, False),
        (411, "MaNhanVien (FK)", 1020, 640, True),  # FK
        (412, "Thang", 1300, 610, False),
        (413, "Nam", 1330, 690, False),
        (414, "LuongCoBan", 1300, 770, False),
        (415, "LuongThucNhan", 1165, 810, False),
    ]
    for aid, name, ax, ay, is_fk in bl_attrs:
        s = fk_attr_style if is_fk else attr_style
        nodes.append(create_node(aid, name, s, ax, ay, 130 if is_fk else 120, 45))
        edges.append(create_edge(3000 + aid, 400, aid, extra_style=attr_edge_style))

    # --- Attributes of TIN TUYỂN DỤNG (500) ---
    ttd_attrs = [
        (510, "<u>MaTinTuyenDung</u>", 60, 780, False),
        (511, "MaPhongBan (FK)", 60, 860, True),  # FK
        (512, "ViTriTuyenDung", 80, 940, False),
        (513, "SoLuongCanTuyen", 230, 970, False),
        (514, "ThoiHanNhanHoSo", 400, 940, False),
        (515, "TrangThai", 420, 840, False),
    ]
    for aid, name, ax, ay, is_fk in ttd_attrs:
        s = fk_attr_style if is_fk else attr_style
        nodes.append(create_node(aid, name, s, ax, ay, 135 if is_fk else 130, 45))
        edges.append(create_edge(3000 + aid, 500, aid, extra_style=attr_edge_style))

    xml_content = (
        '<?xml version="1.0" encoding="UTF-8"?>\n'
        '<mxfile host="app.diagrams.net">\n'
        '  <diagram id="hrm-chen-erd" name="Sơ đồ Chen ERD 5 Thực thể HRM">\n'
        '    <mxGraphModel dx="1600" dy="1200" grid="1" gridSize="10" guides="1" tooltips="1" connect="1" arrows="1" fold="1" page="1" pageScale="1" pageWidth="1600" pageHeight="1100" math="0" shadow="0">\n'
        '      <root>\n'
        '        <mxCell id="0" />\n'
        '        <mxCell id="1" parent="0" />\n'
        + "\n".join(nodes) + "\n"
        + "\n".join(edges) + "\n"
        '      </root>\n'
        '    </mxGraphModel>\n'
        '  </diagram>\n'
        '</mxfile>'
    )

    out_file = os.path.join(os.path.dirname(__file__), "docs", "HRM_ERD_5_Entities.drawio")
    with open(out_file, "w", encoding="utf-8") as f:
        f.write(xml_content)
    print(f"Generated Chen ERD successfully with explicit Foreign Keys (FK) to {out_file}")

if __name__ == "__main__":
    main()

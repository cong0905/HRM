#!/usr/bin/env python3
"""
Generate Draw.io XML for HRM Leave Management diagrams in Classic Black & White style (Trắng đen).
All fills are #FFFFFF, all strokes/lines/text are #000000.
"""

import os
import xml.etree.ElementTree as ET
from xml.dom import minidom

STROKE_COLOR = "#000000"  # Black
FILL_COLOR = "#FFFFFF"    # White
FONT_COLOR = "#000000"    # Black Text
BLACK = "#000000"

def add_vertex(root, cid, value, style, x, y, w, h):
    cell = ET.SubElement(root, "mxCell", id=cid, parent="1", vertex="1", style=style, value=value)
    ET.SubElement(cell, "mxGeometry", x=str(x), y=str(y), width=str(w), height=str(h), **{"as": "geometry"})
    return cell

def add_edge(root, cid, source, target, label="", style="", points=None):
    base_style = f"html=1;strokeColor={STROKE_COLOR};fontColor={FONT_COLOR};fontSize=11;strokeWidth=1.2;{style}"
    cell = ET.SubElement(root, "mxCell", id=cid, parent="1", source=source, target=target, edge="1", style=base_style, value=label)
    geom = ET.SubElement(cell, "mxGeometry", relative="1", **{"as": "geometry"})
    if points:
        pts_arr = ET.SubElement(geom, "Array", **{"as": "points"})
        for pt in points:
            ET.SubElement(pts_arr, "mxPoint", x=str(pt[0]), y=str(pt[1]))
    return cell

def build_state_diagram():
    root = ET.Element("root")
    ET.SubElement(root, "mxCell", id="0")
    ET.SubElement(root, "mxCell", id="1", parent="0")

    # Title
    add_vertex(root, "title_st", "Ví dụ biểu đồ trạng thái - Chức năng Quản lý Nghỉ phép",
               f"text;html=1;strokeColor=none;fillColor=none;align=center;verticalAlign=middle;whiteSpace=wrap;rounded=0;fontColor={FONT_COLOR};fontSize=15;fontStyle=1;",
               300, 580, 500, 30)

    # Start Node
    add_vertex(root, "st_start", "", f"ellipse;html=1;shape=startState;fillColor={BLACK};strokeColor={BLACK};pointerEvents=1;", 80, 120, 30, 30)

    # States
    add_vertex(root, "st_moitao", "MoiTao", f"rounded=1;whiteSpace=wrap;html=1;arcSize=25;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};fontColor={FONT_COLOR};fontStyle=1;fontSize=13;strokeWidth=1.5;", 240, 105, 170, 60)
    add_vertex(root, "st_cho", "Cho - trong mot\nkhoang Thoi gian", f"rounded=1;whiteSpace=wrap;html=1;arcSize=25;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};fontColor={FONT_COLOR};fontStyle=1;fontSize=13;strokeWidth=1.5;", 540, 270, 220, 70)
    add_vertex(root, "st_duyet", "DaPheDuyet", f"rounded=1;whiteSpace=wrap;html=1;arcSize=25;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};fontColor={FONT_COLOR};fontStyle=1;fontSize=13;strokeWidth=1.5;", 240, 460, 170, 60)

    # End Node
    add_vertex(root, "st_end", "", f"ellipse;html=1;shape=endState;fillColor={BLACK};strokeColor={STROKE_COLOR};strokeWidth=2;pointerEvents=1;", 840, 465, 40, 40)

    # Edges
    add_edge(root, "e1", "st_start", "st_moitao", "Khoi tao", "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;endArrow=classic;")
    add_edge(root, "e2", "st_moitao", "st_duyet", "Gui don - Du dieu kien duyet ngay", "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;endArrow=classic;exitX=0.5;exitY=1;entryX=0.5;entryY=0;")
    add_edge(root, "e3", "st_moitao", "st_cho", "Gui don - Khong du dieu kien duyet ngay", "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;endArrow=classic;exitX=0.8;exitY=1;entryX=0;entryY=0.3;")
    add_edge(root, "e4", "st_cho", "st_cho", "Kiem tra don nghi", "edgeStyle=loopEdgeStyle;dy=-20;dx=30;direction=north;endArrow=classic;")
    add_edge(root, "e5", "st_cho", "st_duyet", "Quan ly duyet", "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;endArrow=classic;exitX=0.2;exitY=1;entryX=1;entryY=0.5;")
    add_edge(root, "e6", "st_cho", "st_end", "Het thoi gian cho", "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;endArrow=classic;exitX=0.8;exitY=1;entryX=0.5;entryY=0;")
    add_edge(root, "e7", "st_moitao", "st_end", "Xoa don - Huy don", "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;endArrow=classic;exitX=1;exitY=0.3;entryX=0.5;entryY=0;", [(860, 123)])
    add_edge(root, "e8", "st_duyet", "st_end", "", "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;endArrow=classic;exitX=1;exitY=0.5;entryX=0;entryY=0.5;")

    return root

def build_sequence_diagram():
    root = ET.Element("root")
    ET.SubElement(root, "mxCell", id="0")
    ET.SubElement(root, "mxCell", id="1", parent="0")

    # Title
    add_vertex(root, "title_seq", "Ví dụ biểu đồ tuần tự - Chức năng Quản lý Nghỉ phép",
               f"text;html=1;strokeColor=none;fillColor=none;align=center;verticalAlign=middle;whiteSpace=wrap;rounded=0;fontColor={FONT_COLOR};fontSize=15;fontStyle=1;",
               300, 720, 500, 30)

    # Lifelines Head
    add_vertex(root, "lf_actor", ": Nhan vien", f"shape=umlActor;verticalLabelPosition=bottom;verticalAlign=top;html=1;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};fontColor={FONT_COLOR};fontStyle=1;fontSize=12;", 100, 40, 40, 60)
    add_vertex(root, "lf_form", ": FormGuiNghiPhep", f"shape=umlBoundary;whiteSpace=wrap;html=1;verticalLabelPosition=bottom;verticalAlign=top;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};fontColor={FONT_COLOR};fontStyle=1;fontSize=12;", 320, 40, 60, 50)
    add_vertex(root, "lf_ctrl", ": DK_NghiPhep", f"shape=umlControl;whiteSpace=wrap;html=1;verticalLabelPosition=bottom;verticalAlign=top;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};fontColor={FONT_COLOR};fontStyle=1;fontSize=12;", 570, 40, 50, 50)
    add_vertex(root, "lf_entity", ": DonNghiPhep", f"shape=umlEntity;whiteSpace=wrap;html=1;verticalLabelPosition=bottom;verticalAlign=top;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};fontColor={FONT_COLOR};fontStyle=1;fontSize=12;", 810, 40, 50, 50)

    # Lifelines Vertical Lines
    add_edge(root, "lf_actor_line", "lf_actor", "lf_actor", "", "strokePattern=dashed;dashed=1;endArrow=none;startArrow=none;", [(120, 100), (120, 680)])
    add_edge(root, "lf_form_line", "lf_form", "lf_form", "", "strokePattern=dashed;dashed=1;endArrow=none;startArrow=none;", [(350, 100), (350, 680)])
    add_edge(root, "lf_ctrl_line", "lf_ctrl", "lf_ctrl", "", "strokePattern=dashed;dashed=1;endArrow=none;startArrow=none;", [(595, 100), (595, 680)])
    add_edge(root, "lf_entity_line", "lf_entity", "lf_entity", "", "strokePattern=dashed;dashed=1;endArrow=none;startArrow=none;", [(835, 100), (835, 680)])

    # Activation Boxes
    add_vertex(root, "act_a1", "", f"html=1;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};strokeWidth=1.2;", 113, 120, 14, 40)
    add_vertex(root, "act_a2", "", f"html=1;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};strokeWidth=1.2;", 113, 200, 14, 450)

    add_vertex(root, "act_b1", "", f"html=1;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};strokeWidth=1.2;", 343, 120, 14, 100)
    add_vertex(root, "act_b2", "", f"html=1;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};strokeWidth=1.2;", 343, 200, 14, 420)

    add_vertex(root, "act_c1", "", f"html=1;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};strokeWidth=1.2;", 588, 250, 14, 260)
    add_vertex(root, "act_c2", "", f"html=1;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};strokeWidth=1.2;", 588, 550, 14, 50)

    add_vertex(root, "act_d1", "", f"html=1;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};strokeWidth=1.2;", 828, 380, 14, 60)
    add_vertex(root, "act_d2", "", f"html=1;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};strokeWidth=1.2;", 828, 480, 14, 90)

    # Messages
    add_edge(root, "m1", "act_a1", "act_b1", "Yeu cau Gui don nghi", "endArrow=block;endFill=1;", [(127, 130), (343, 130)])
    add_edge(root, "m2", "act_b1", "act_a1", "Yeu cau nhap thong tin", "endArrow=open;dashed=1;strokePattern=dashed;", [(343, 180), (127, 180)])
    add_edge(root, "m3", "act_a2", "act_b2", "Nhap thong tin don nghi phep moi", "endArrow=block;endFill=1;", [(127, 220), (343, 220)])
    add_edge(root, "m4", "act_b2", "act_c1", "Tao doi tuong don nghi phep moi", "endArrow=block;endFill=1;", [(357, 260), (588, 260)])
    add_edge(root, "m5", "act_c1", "act_c1", "Kiem tra thong tin don nghi", "edgeStyle=orthogonalEdgeStyle;rounded=0;endArrow=block;endFill=1;", [(602, 300), (642, 300), (642, 340), (602, 340)])
    add_edge(root, "m6", "act_c1", "act_d1", "Luu don nghi phep vao CSDL", "endArrow=block;endFill=1;", [(602, 390), (828, 390)])
    add_edge(root, "m7", "act_d2", "act_c2", "Luu thanh cong", "endArrow=open;dashed=1;strokePattern=dashed;", [(828, 520), (602, 520)])
    add_edge(root, "m8", "act_c2", "act_b2", "Luu thanh cong", "endArrow=open;dashed=1;strokePattern=dashed;", [(588, 570), (357, 570)])
    add_edge(root, "m9", "act_b2", "act_a2", "Thong bao gui don thanh cong", "endArrow=open;dashed=1;strokePattern=dashed;", [(343, 610), (127, 610)])

    return root

def build_activity_diagram():
    root = ET.Element("root")
    ET.SubElement(root, "mxCell", id="0")
    ET.SubElement(root, "mxCell", id="1", parent="0")

    # Title
    add_vertex(root, "title_act", "Ví dụ biểu đồ hoạt động - Chức năng Quản lý Nghỉ phép",
               f"text;html=1;strokeColor=none;fillColor=none;align=center;verticalAlign=middle;whiteSpace=wrap;rounded=0;fontColor={FONT_COLOR};fontSize=15;fontStyle=1;",
               300, 840, 500, 30)

    # 1. Activity: Nhan thong tin Don nghi phep
    add_vertex(root, "act1", "Nhan thong tin\nDon nghi phep", f"rounded=1;whiteSpace=wrap;html=1;arcSize=40;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};fontColor={FONT_COLOR};fontStyle=1;fontSize=12;strokeWidth=1.5;", 375, 60, 210, 55)

    # Top arrow in
    add_edge(root, "top_in", "act1", "act1", "", "endArrow=classic;", [(480, 20), (480, 60)])
    add_edge(root, "e_a1_f1", "act1", "fork1", "", "endArrow=classic;", [(480, 115), (480, 150)])

    # Fork Bar 1
    add_vertex(root, "fork1", "", f"shape=rectangle;fillColor={BLACK};strokeColor=none;", 260, 150, 440, 8)

    # Branch 1 Left & Right
    add_vertex(root, "b1_left", "Kiem tra\nNhan vien", f"rounded=1;whiteSpace=wrap;html=1;arcSize=40;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};fontColor={FONT_COLOR};fontStyle=1;fontSize=12;strokeWidth=1.5;", 210, 190, 180, 50)
    add_vertex(root, "b1_right", "Kiem tra thong tin\nDon nghi", f"rounded=1;whiteSpace=wrap;html=1;arcSize=40;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};fontColor={FONT_COLOR};fontStyle=1;fontSize=12;strokeWidth=1.5;", 570, 190, 190, 50)

    add_edge(root, "e_f1_l", "fork1", "b1_left", "", "endArrow=classic;", [(300, 158), (300, 190)])
    add_edge(root, "e_f1_r", "fork1", "b1_right", "", "endArrow=classic;", [(665, 158), (665, 190)])

    # Join Bar 1
    add_vertex(root, "join1", "", f"shape=rectangle;fillColor={BLACK};strokeColor=none;", 260, 280, 440, 8)
    add_edge(root, "e_l_j1", "b1_left", "join1", "Xac nhan dung", "endArrow=classic;", [(300, 240), (300, 280)])
    add_edge(root, "e_r_j1", "b1_right", "join1", "Xac nhan dung", "endArrow=classic;", [(665, 240), (665, 280)])

    # Activity 2: Gui yeu cau Nghi phep
    add_vertex(root, "act2", "Gui yeu cau\nNghi phep", f"rounded=1;whiteSpace=wrap;html=1;arcSize=40;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};fontColor={FONT_COLOR};fontStyle=1;fontSize=12;strokeWidth=1.5;", 375, 320, 210, 55)
    add_edge(root, "e_j1_a2", "join1", "act2", "", "endArrow=classic;", [(480, 288), (480, 320)])

    # Fork Bar 2
    add_vertex(root, "fork2", "", f"shape=rectangle;fillColor={BLACK};strokeColor=none;", 240, 415, 480, 8)
    add_edge(root, "e_a2_f2", "act2", "fork2", "", "endArrow=classic;", [(480, 375), (480, 415)])

    # Branch 2 Left & Right
    add_vertex(root, "b2_left", "Kiem tra so ngay phep\nNhan vien da nghi", f"rounded=1;whiteSpace=wrap;html=1;arcSize=40;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};fontColor={FONT_COLOR};fontStyle=1;fontSize=12;strokeWidth=1.5;", 130, 455, 240, 55)
    add_vertex(root, "b2_right", "Kiem tra quyen han\nva lich cong tac", f"rounded=1;whiteSpace=wrap;html=1;arcSize=40;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};fontColor={FONT_COLOR};fontStyle=1;fontSize=12;strokeWidth=1.5;", 590, 455, 240, 55)

    add_edge(root, "e_f2_l", "fork2", "b2_left", "", "endArrow=classic;", [(250, 423), (250, 455)])
    add_edge(root, "e_f2_r", "fork2", "b2_right", "", "endArrow=classic;", [(710, 423), (710, 455)])

    # Join Bar 2
    add_vertex(root, "join2", "", f"shape=rectangle;fillColor={BLACK};strokeColor=none;", 260, 550, 440, 8)
    add_edge(root, "e_l_j2", "b2_left", "join2", "Xac nhan quyen nghi phep", "fontSize=10;endArrow=classic;", [(300, 510), (300, 550)])
    add_edge(root, "e_r_j2", "b2_right", "join2", "Xac nhan du dieu kien", "fontSize=10;endArrow=classic;", [(665, 510), (665, 550)])

    # Final Node
    add_vertex(root, "final_act", "", f"ellipse;html=1;shape=endState;fillColor={BLACK};strokeColor={STROKE_COLOR};strokeWidth=2;pointerEvents=1;", 460, 770, 40, 40)

    # Failure Paths
    add_edge(root, "e_fail_l", "b2_left", "final_act", "Het so ngay phep nam", "edgeStyle=orthogonalEdgeStyle;rounded=0;endArrow=classic;", [(150, 510), (150, 790), (460, 790)])
    add_edge(root, "e_fail_r", "b2_right", "final_act", "Khong du dieu kien nghi phep", "edgeStyle=orthogonalEdgeStyle;rounded=0;endArrow=classic;", [(810, 510), (810, 790), (500, 790)])

    # Activity 3: Xac nhan cho nghi phep
    add_vertex(root, "act3", "Xac nhan cho\nnghi phep", f"rounded=1;whiteSpace=wrap;html=1;arcSize=40;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};fontColor={FONT_COLOR};fontStyle=1;fontSize=12;strokeWidth=1.5;", 385, 590, 190, 50)
    add_edge(root, "e_j2_a3", "join2", "act3", "", "endArrow=classic;", [(480, 558), (480, 590)])

    # Activity 4: In Phieu nghi phep
    add_vertex(root, "act4", "In Phieu\nnghi phep", f"rounded=1;whiteSpace=wrap;html=1;arcSize=40;fillColor={FILL_COLOR};strokeColor={STROKE_COLOR};fontColor={FONT_COLOR};fontStyle=1;fontSize=12;strokeWidth=1.5;", 385, 675, 190, 50)
    add_edge(root, "e_a3_a4", "act3", "act4", "", "endArrow=classic;", [(480, 640), (480, 675)])

    add_edge(root, "e_a4_final", "act4", "final_act", "", "endArrow=classic;", [(480, 725), (480, 770)])

    return root

def write_single_file(filename, pname, proot):
    mxfile = ET.Element("mxfile", host="app.diagrams.net")
    diag = ET.SubElement(mxfile, "diagram", id="page_1", name=pname)
    graph_model = ET.SubElement(diag, "mxGraphModel", dx="1000", dy="1000", grid="1", gridSize="10",
                                 guides="1", tooltips="1", connect="1", arrows="1", fold="1", page="1",
                                 pageScale="1", pageWidth="1169", pageHeight="900", math="0", shadow="0")
    graph_model.append(proot)
    xml_str = minidom.parseString(ET.tostring(mxfile, encoding="utf-8")).toprettyxml(indent="  ")
    out_path = os.path.join(r"d:\dotnet project\db\docs", filename)
    with open(out_path, "w", encoding="utf-8") as f:
        f.write(xml_str)
    print(f"Generated {filename}")

def main():
    # 1. Multi-tab file
    mxfile = ET.Element("mxfile", host="app.diagrams.net")

    pages = [
        ("Sơ đồ Trạng thái (State Diagram)", build_state_diagram()),
        ("Sơ đồ Tuần tự (Sequence Diagram)", build_sequence_diagram()),
        ("Sơ đồ Hoạt động (Activity Diagram)", build_activity_diagram()),
    ]

    for idx, (pname, proot) in enumerate(pages):
        diag = ET.SubElement(mxfile, "diagram", id=f"page_{idx+1}", name=pname)
        graph_model = ET.SubElement(diag, "mxGraphModel", dx="1000", dy="1000", grid="1", gridSize="10",
                                     guides="1", tooltips="1", connect="1", arrows="1", fold="1", page="1",
                                     pageScale="1", pageWidth="1169", pageHeight="900", math="0", shadow="0")
        graph_model.append(proot)

    xml_str = minidom.parseString(ET.tostring(mxfile, encoding="utf-8")).toprettyxml(indent="  ")
    out_path = r"d:\dotnet project\db\docs\HRM_ChucNang_NghiPhep_Diagrams.drawio"
    with open(out_path, "w", encoding="utf-8") as f:
        f.write(xml_str)

    # 2. Individual single files
    write_single_file("SoDo_TrangThai_NghiPhep.drawio", "Sơ đồ Trạng thái", build_state_diagram())
    write_single_file("SoDo_TuanTu_NghiPhep.drawio", "Sơ đồ Tuần tự", build_sequence_diagram())
    write_single_file("SoDo_HoatDong_NghiPhep.drawio", "Sơ đồ Hoạt động", build_activity_diagram())

    print("All files successfully updated to Black & White theme!")

if __name__ == "__main__":
    main()

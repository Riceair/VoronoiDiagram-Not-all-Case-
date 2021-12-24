using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace VoronoiDiagram
{
    public class DiagramCalculator: Object
    {
        private Queue<List<PointEdgeRecoder>> recoder_buffer = new Queue<List<PointEdgeRecoder>>(); //紀錄執行步驟的buffer(以處理step模式)
        private CalMath calMath; //數學公式的物件
        private float max_number;
        private TextBox textBox; //Debug

        public DiagramCalculator(float max_number){
            this.max_number = max_number;
            calMath = new CalMath(max_number);
        }

        public void setTextBox(TextBox text){ //Debug
            textBox = text;
        }

        public PointEdgeRecoder run(List<PointF> points_list){
            points_list = calMath.getSortPoints(points_list); //先對points進行排序
            recoder_buffer.Clear();
            PointEdgeRecoder recod_result = runVoronoiDiagram(points_list); //執行遞迴式
            recod_result.points_list = calMath.getSortPoints(recod_result.points_list);
            recod_result.edges_list = calMath.getSortEdges(recod_result.edges_list);
            return recod_result;
        }

        private PointEdgeRecoder runVoronoiDiagram(List<PointF> pList){ //遞迴
            int count = pList.Count; //紀錄當前的點個數

            if(count <= 3){ //直接做Voroni Diagram
                PointEdgeRecoder point_edge;
                List<Edge> eList = new List<Edge>();

                if(count == 1) //若只有1個，直接返回點
                    point_edge = new PointEdgeRecoder(pList);
                else if(count==2){ //若只有2個，畫中垂線再return
                    Edge edge = calMath.getVoronoiEdge(pList[0], pList[1]); //取得中垂線兩點
                    eList.Add(edge);
                    point_edge = new PointEdgeRecoder(pList, eList);
                }
                else{ //若有3個點
                    /*外心為3中垂線交點(找2個中垂線交點即可)*/
                    if(calMath.is3ALine(pList[0], pList[1], pList[2])){ //檢查是否三點共線
                        eList.Add(calMath.getVoronoiEdge(pList[0], pList[1])); //三點共線直接加入兩個邊
                        eList.Add(calMath.getVoronoiEdge(pList[1], pList[2]));
                    }
                    else{ //沒有三點共線，找重心
                        List<PointF> pList_sort = calMath.getCounterClockwiseSortPoints(pList); //確保點逆時鐘排序
                        
                        PointF centroid = calMath.getIntersection(calMath.getVoronoiEdge(pList_sort[0], pList_sort[1]), calMath.getVoronoiEdge(pList_sort[1], pList_sort[2])); //找重心
                        //若經過排序，重心往中垂線向量的方向就是要保留的邊
                        PointF vertVec_0_1 = calMath.getVerticalVec(calMath.getVector(pList_sort[0], pList_sort[1])); //點0 1的中垂線向量
                        PointF vertVec_1_2 = calMath.getVerticalVec(calMath.getVector(pList_sort[1], pList_sort[2])); //點1 2的中垂線向量
                        PointF vertVec_2_0 = calMath.getVerticalVec(calMath.getVector(pList_sort[2], pList_sort[0])); //點2 0的中垂線向量
                        //加入Voroni的Edge points (centroid+vector*max_number, center)
                        eList.Add(new Edge(calMath.addPoints(centroid, calMath.multPoints(vertVec_0_1, max_number)), centroid, pList_sort[0], pList_sort[1]));
                        eList.Add(new Edge(calMath.addPoints(centroid, calMath.multPoints(vertVec_1_2, max_number)), centroid, pList_sort[1], pList_sort[2]));
                        eList.Add(new Edge(calMath.addPoints(centroid, calMath.multPoints(vertVec_2_0, max_number)), centroid, pList_sort[2], pList_sort[0]));
                    }
                    point_edge  = new PointEdgeRecoder(pList, eList);
                }
                
                point_edge.point_color = Color.Blue;
                point_edge.edge_color = Color.LightBlue;
                //存Buffer
                addToBuffer(point_edge); //記錄到buffer中
                return (PointEdgeRecoder) point_edge.getClone(); //用clone的物件
            }
            else{
                //左右兩邊分別跑VoronoiDiagram遞迴
                PointEdgeRecoder left_PE, right_PE;
                if(count == 5){ //若有5個點，看中間那個點離左邊還右邊較近就分過去(這樣分較為合理)
                    int split_count;
                    if(calMath.getPointDistance(pList[1], pList[2]) < calMath.getPointDistance(pList[2], pList[3]))
                        split_count = 3;
                    else split_count = 2;
                    
                    left_PE = runVoronoiDiagram(pList.GetRange(0, split_count));
                    right_PE = runVoronoiDiagram(pList.GetRange(split_count, pList.Count - split_count));
                }
                else{
                    left_PE = runVoronoiDiagram(pList.GetRange(0, pList.Count/2));
                    right_PE = runVoronoiDiagram(pList.GetRange(pList.Count/2, pList.Count - pList.Count/2));
                }

                //存Buffer
                //取得左右邊的convex hull
                left_PE.edge_color = Color.DarkBlue;
                right_PE.edge_color = Color.Orange;
                left_PE.convex_list = calMath.getConvexHull(left_PE.points_list);
                right_PE.convex_list = calMath.getConvexHull(right_PE.points_list);
                addToBuffer(left_PE, right_PE); //記錄到buffer中
                
                //對所有的點找Convex Hull
                PointEdgeRecoder all_PE = new PointEdgeRecoder(new List<PointF>(left_PE.points_list)); //先複製左半部的點
                all_PE.points_list.AddRange(right_PE.points_list); //再將右半部的點複製加入
                List<Edge> ch_upper = calMath.getConvexUpper(all_PE.points_list); //找Convex Hull上包
                List<Edge> ch_lower = calMath.getConvexLower(all_PE.points_list); //找Convex Hull下包

                //存Buffer
                //針對找到所有Convex Hull存入buffer(為了顯示左右側，因此加入的是左右側的recoder)
                left_PE.convex_list = ch_upper;
                right_PE.convex_list = ch_lower;
                addToBuffer(left_PE, right_PE);

                Edge convex_upper_line = calMath.getPointDiffSideEdge(ch_upper, left_PE.points_list, right_PE.points_list); //Convex Hull上方橫跨左右側的邊
                Edge convex_lower_line = calMath.getPointDiffSideEdge(ch_lower, left_PE.points_list, right_PE.points_list); //Convex Hull下方橫跨左右側的邊

                Edge hyper_voronoi_edge = calMath.getVoronoiEdge(convex_upper_line.edgePA, convex_upper_line.edgePB); //取得hyper edge
                //複製左側的邊
                List<Edge> left_edges = left_PE.edges_list.ConvertAll(edge => (Edge) edge.getClone());
                //複製右側的邊
                List<Edge> right_edges = right_PE.edges_list.ConvertAll(edge => (Edge) edge.getClone());

                PointF start_left, start_right, end_left, end_right; //儲存merge的左右邊開始結束點
                 //確定start point左右側是真的存左右側
                if(convex_upper_line.edgePA.X < convex_upper_line.edgePB.X){
                    start_left = convex_upper_line.edgePA;
                    start_right = convex_upper_line.edgePB;
                }
                else{
                    start_left = convex_upper_line.edgePB;
                    start_right = convex_upper_line.edgePA;
                }
                if(convex_lower_line.edgePA.X < convex_lower_line.edgePB.X){
                    end_left = convex_lower_line.edgePA;
                    end_right = convex_lower_line.edgePB;
                }
                else{
                    end_left = convex_lower_line.edgePB;
                    end_right = convex_lower_line.edgePA;
                }

                PointF current_left = start_left, current_right = start_right;
                Edge current_edge = hyper_voronoi_edge;
                List<Edge> modify_edges = new List<Edge>(); //儲存要修改的Edge
                List<Edge> left_modify_edges = new List<Edge>(); //"紀錄"有修改過的左側Edge
                List<Edge> right_modify_edges = new List<Edge>(); //"紀錄"有修改過的右側Edge
                List<Edge> m_eList = new List<Edge>(); //儲存修改後的Edge
                List<int> modify_side_list = new List<int>(); //儲存修改哪邊

                //畫hyper plane
                int last_side = 0; //紀錄上一次是碰到哪一邊 1左 2右 3兩邊同交點

                for(int c=0;c<count*3;c++){ //用所有點的數當迴圈數，避免bug卡住
                    all_PE.hyper_list.Add((Edge)current_edge.getClone()); //先將hyper plane加入

                    Edge left_first = new Edge(); //紀錄hyper plane第一個碰到的edge
                    PointF left_intersection = new PointF(); //紀錄交點
                    bool isLeftFound = false;
                    foreach(Edge edge in left_edges){ //找到左側第一個碰到的edge

                        PointF edge_intersection = calMath.getIntersection(edge, current_edge);
                        if(edge_intersection.Y > Math.Max(edge.edgePA.Y, edge.edgePB.Y) //交點不在edge上，表示實際上沒交點
                        || edge_intersection.Y < Math.Min(edge.edgePA.Y, edge.edgePB.Y)
                        || edge_intersection.X > Math.Max(edge.edgePA.X, edge.edgePB.X)
                        || edge_intersection.X < Math.Min(edge.edgePA.X, edge.edgePB.X))
                            continue;

                        if(edge_intersection.Y > current_edge.edgePA.Y) //找到回頭點，跳過
                            continue;

                        if(edge_intersection.Y == current_edge.edgePA.Y && (last_side==1 || last_side==3)) //找到上一個點
                            continue;

                        if(edge_intersection.Y > left_intersection.Y){
                            left_first = edge;
                            left_intersection = edge_intersection;
                            isLeftFound = true;
                        }
                    }

                    Edge right_first = new Edge(); //紀錄hyper plane第一個碰到的edge
                    PointF right_intersection = new PointF(); //紀錄交點
                    bool isRightFound = false;
                    foreach(Edge edge in right_edges){ //找到右側第一個碰到的edge

                        PointF edge_intersection = calMath.getIntersection(edge, current_edge);
                        if(edge_intersection.Y > Math.Max(edge.edgePA.Y, edge.edgePB.Y) //交點不在edge上，表示實際上沒交點
                        || edge_intersection.Y < Math.Min(edge.edgePA.Y, edge.edgePB.Y)
                        || edge_intersection.X > Math.Max(edge.edgePA.X, edge.edgePB.X)
                        || edge_intersection.X < Math.Min(edge.edgePA.X, edge.edgePB.X))
                            continue;
                        if(edge_intersection.Y > current_edge.edgePA.Y) //找到回頭點，跳過
                            continue;
                        
                        if(edge_intersection.Y == current_edge.edgePA.Y && (last_side==2 || last_side==3)) //找到上一個點
                            continue;

                        if(edge_intersection.Y > right_intersection.Y){
                            right_first = edge;
                            right_intersection = edge_intersection;
                            isRightFound = true;
                        }
                    }

                    if(!isLeftFound && !isRightFound){
                        break;
                    }

                    PointF intersection;
                    if(left_intersection.Equals(right_intersection)){ //左右交點相同->線直接畫過去(但兩邊都要砍)
                        intersection = left_intersection;
                        if(current_left.Y >= left_first.pointA.Y)
                            current_left = left_first.pointB;
                        else
                            current_left = left_first.pointA;
                        
                        if(current_right.Y >= right_first.pointA.Y)
                            current_right = right_first.pointB;
                        else
                            current_right = right_first.pointA;

                        if(intersection.Y == calMath.getCenterPoint(left_first.pointA, left_first.pointB).Y)
                            left_first.edgePB = intersection;
                        else
                            left_first.edgePA = intersection;
                        
                        right_first.edgePA = intersection;

                        m_eList.Add(left_first);
                        m_eList.Add(right_first);
                        last_side = 3;
                    }
                    else if(left_intersection.Y > right_intersection.Y){ //判斷先交右側還是左側
                        intersection = left_intersection;
                        if(current_left.Equals(left_first.pointA))
                            current_left = left_first.pointB;
                        else
                            current_left = left_first.pointA;

                        modify_edges.Add(left_first);
                        left_modify_edges.Add(left_first);
                        modify_side_list.Add(1);
                        last_side = 1;
                    }
                    else{
                        intersection = right_intersection;
                        if(current_right.Equals(right_first.pointA))
                            current_right = right_first.pointB;
                        else
                            current_right = right_first.pointA;

                        modify_edges.Add(right_first);
                        right_modify_edges.Add(right_first);
                        modify_side_list.Add(2);
                        last_side = 2;
                    }

                    //修正前一個 hyper plane長度
                    all_PE.hyper_list.Last().edgePB = intersection;

                    current_edge = calMath.getVoronoiEdge(current_left, current_right);
                    current_edge.edgePA = intersection;
                }

                //eL.Add(bottom_edge);
                addToBuffer(left_PE, right_PE, (PointEdgeRecoder)all_PE.getClone());

                //紀錄結果和Hyper Plane
                all_PE.edges_list = m_eList;
                List<Edge> modify_edges_list = calMath.getModifyEdges(modify_edges, all_PE.hyper_list);
                foreach(Edge edge in left_modify_edges){
                    left_edges.Remove(edge);
                }
                foreach(Edge edge in right_modify_edges){
                    right_edges.Remove(edge);
                }
                for(int i=0;i<modify_side_list.Count;i++){
                    if(modify_side_list[i]==1){
                        left_edges.Add(modify_edges_list[i]);
                    }
                    else{
                        right_edges.Add(modify_edges_list[i]);
                    }
                }

                List<Edge> left_remain_list = calMath.getRemoveNoLinkEdges(left_edges, modify_edges_list);
                List<Edge> right_remain_list = calMath.getRemoveNoLinkEdges(right_edges, modify_edges_list);
                all_PE.edges_list.AddRange(left_remain_list); //去除完全沒相交的線
                all_PE.edges_list.AddRange(right_remain_list); //去除完全沒相交的線
                //all_PE.edges_list.AddRange(modify_edges_list);
                addToBuffer((PointEdgeRecoder) all_PE.getClone());

                //最終結果存入buffer
                all_PE.edges_list.AddRange(all_PE.hyper_list);
                all_PE.hyper_list = new List<Edge>();
                addToBuffer((PointEdgeRecoder) all_PE.getClone());

                return all_PE;
            }
        }

        public Queue<List<PointEdgeRecoder>> getRecoderBuffer(){
            return recoder_buffer;
        }

        private void addToBuffer(PointEdgeRecoder recoder){
            List<PointEdgeRecoder> PE_recoder = new List<PointEdgeRecoder>();
            PE_recoder.Add((PointEdgeRecoder) recoder.getClone());
            recoder_buffer.Enqueue(PE_recoder);
        }

        private void addToBuffer(PointEdgeRecoder recoder1, PointEdgeRecoder recoder2){
            List<PointEdgeRecoder> PE_recoder = new List<PointEdgeRecoder>();
            PE_recoder.Add((PointEdgeRecoder) recoder1.getClone());
            PE_recoder.Add((PointEdgeRecoder) recoder2.getClone());
            recoder_buffer.Enqueue(PE_recoder);
        }

        private void addToBuffer(PointEdgeRecoder recoder1, PointEdgeRecoder recoder2, PointEdgeRecoder recoder3){
            List<PointEdgeRecoder> PE_recoder = new List<PointEdgeRecoder>();
            PE_recoder.Add((PointEdgeRecoder) recoder1.getClone());
            PE_recoder.Add((PointEdgeRecoder) recoder2.getClone());
            PE_recoder.Add((PointEdgeRecoder) recoder3.getClone());
            recoder_buffer.Enqueue(PE_recoder);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace VoronoiDiagram
{
    public class DiagramCalculator: Object
    {
        private List<PointEdgeRecoder> recoder_buffer = new List<PointEdgeRecoder>(); //紀錄執行步驟的buffer(以處理step模式)
        private float maxX, maxY;
        private TextBox textBox; //Debug

        public DiagramCalculator(float maxX, float maxY){
            this.maxX = maxX;
            this.maxY = maxY;
        }

        public void setTextBox(TextBox text){ //Debug
            textBox = text;
        }

        public PointEdgeRecoder run(List<PointF> points_list){
            //需要補做排序功能
            //
            recoder_buffer.Clear();
            return runVoronoiDiagram(points_list); //執行遞迴式
        }

        private PointEdgeRecoder runVoronoiDiagram(List<PointF> pList){ //遞迴
            int count = pList.Count; //紀錄當前的點個數
            if(count <= 3){ //直接做Voroni Diagram
                PointEdgeRecoder point_edge;
                List<Edge> eList = new List<Edge>();

                if(count == 1) //若只有1個，直接返回點
                    point_edge = new PointEdgeRecoder(pList);
                else if(count==2){ //若只有2個，畫中垂線再return
                    List<PointF> edge_points = getEdgePoints(pList[0], pList[1]); //取得中垂線兩點
                    Edge edge = new Edge(edge_points, pList);
                    eList.Add(edge);
                    point_edge = new PointEdgeRecoder(pList, eList);
                }
                else{ //若有3個點，找外心
                    //外心為3中垂線交點(找2個中垂線交點即可)
                    List<PointF> edge_points1 = getEdgePoints(pList[0], pList[1]); //取0 1兩點的中垂線
                    List<PointF> edge_points2 = getEdgePoints(pList[1], pList[2]); //取1 2兩點的中垂線
                    List<PointF> edge_points3 = getEdgePoints(pList[0], pList[2]); //取0 2兩點的中垂線
                    PointF exocentric = getIntersection(edge_points1, edge_points2); //外心(求兩線交點)
                    
                    updateEdgePoints(edge_points1, exocentric); //更新三個外心
                    updateEdgePoints(edge_points2, exocentric);
                    updateEdgePoints(edge_points3, exocentric);

                    eList.Add(new Edge(edge_points1, pList.GetRange(0,2))); //建立三個edge物件
                    eList.Add(new Edge(edge_points2, pList.GetRange(1,2)));
                    List<PointF> pList_02 = new List<PointF>();
                    pList_02.Add(pList[0]);
                    pList_02.Add(pList[2]);
                    eList.Add(new Edge(edge_points3, pList_02));
                    point_edge = new PointEdgeRecoder(pList, eList);
                }

                recoder_buffer.Add(point_edge); //記錄到buffer中
                return point_edge;
            }
            else{
                textBox.AppendText("尚無法處理大於三個的情況");
                return null;
            }
        }

        private List<PointF> getEdgePoints(PointF point1, PointF point2){ //取得兩點之中垂線
            PointF center_point = getCenterPoint(point1, point2); //找兩點的中心點
            float slope = -1/getSlope(point1, point2); //取得中垂線斜率 y=ax+b的a
            float cons = getConst(center_point, slope); //取得方程式常數 y=ax+b的b

            if(getEquationY(0,slope,cons) > maxY){ //求edge的第一個點
                point1 = new PointF(getEquationX(maxY, slope, cons), maxY);
            }
            else{
                point1 = new PointF(0, getEquationY(0, slope, cons));
            }
            if(getEquationX(0,slope,cons) > maxX){ //求edge的第二個點
                point2 = new PointF(maxX, getEquationY(maxX, slope, cons));
            }
            else{
                point2 = new PointF(getEquationX(0,slope,cons), 0);
            }
            List<PointF> edge_points = new List<PointF>();
            edge_points.Add(point1);
            edge_points.Add(point2);
            return edge_points;
        }
        private PointF getCenterPoint(PointF point1, PointF point2){ //算中心點
            float x = (point1.X + point2.X)/2;
            float y = (point1.Y + point2.Y)/2;
            return new PointF(x, y);
        }
        private float getSlope(PointF point1, PointF point2){ //算斜率
            return (point2.Y - point1.Y)/(point2.X - point1.X);
        }
        private float getConst(PointF point, float slope){ //算方程式常數
            return point.Y - slope*point.X;
        }
        private float getEquationY(float x, float slope, float cons){ //餵入x, slope, const 求y
            return slope*x+cons;
        }
        private float getEquationX(float y, float slope, float cons){ //餵入y, slope, const 求x
            return (y-cons)/slope;
        }
        // int cross(Vector v1, Vector v2)
        // {
        //     return v1.x * v2.y - v1.y * v2.x;
        // }
        //
        // 交點參考程式
        // Point intersection(Point a1, Point a2, Point b1, Point b2)
        // {
        //     Point a = a2 - a1, b = b2 - b1, s = b1 - a1;
        //     // 計算交點
        //     return a1 + a * cross(s, b) / cross(a, b);
        // }
        private PointF getIntersection(List<PointF> pList1, List<PointF> pList2){ //找兩線交點
            PointF a = new PointF(pList1[1].X-pList1[0].X, pList1[1].Y-pList1[0].Y);
            PointF b = new PointF(pList2[1].X-pList2[0].X, pList2[1].Y-pList2[0].Y);
            PointF s = new PointF(pList2[0].X-pList1[0].X, pList2[0].Y-pList1[0].Y);
            float cross_sb = s.X*b.Y - s.Y*b.X;
            float cross_ab = a.X*b.Y - a.Y*b.X;
            a.X = a.X*cross_sb/cross_ab;
            a.Y = a.Y*cross_sb/cross_ab;
            return new PointF(pList1[0].X+a.X, pList1[0].Y+a.Y);
        }
        private float getPointDistance(PointF point1, PointF point2){ //運算兩點距離
            return (float)Math.Sqrt(Math.Pow(point1.X-point2.X,2)+Math.Pow(point1.Y-point2.Y,2));
        }
        private void updateEdgePoints(List<PointF> points, PointF exocentric){
            if(exocentric.X < 0 || exocentric.X > maxX || exocentric.Y < 0 || exocentric.Y > maxY) //若外心在圖外，不更新點
                    return;
            if(getPointDistance(points[0], exocentric) > getPointDistance(points[1], exocentric)){
                points[0] = exocentric;
            }
            else{
                points[1] = exocentric;
            }
        }
    }
}

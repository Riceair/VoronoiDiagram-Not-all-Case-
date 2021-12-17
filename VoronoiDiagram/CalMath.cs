using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace VoronoiDiagram
{
    public class CalMath: Object
    {
        private float max_number;

        public CalMath(float max_number){
            this.max_number = max_number;
        }

        public List<PointF> getSortPoints(List<PointF> pList) //先根據X再依據Y排序
        {
            pList = pList.OrderBy(A => A.X).ThenBy(B => B.Y).ToList();
            return pList;
        }
        public List<Edge> getSortEdges(List<Edge> eList){ //排序edge
            foreach(Edge edge in eList){ //先排序單一邊的兩點
                if(edge.edgePA.X > edge.edgePB.X || (edge.edgePA.X == edge.edgePB.X && edge.edgePA.Y > edge.edgePB.Y)){
                    PointF tmp = edge.edgePB;
                    edge.edgePB = edge.edgePA;
                    edge.edgePA = tmp;
                }
            }

            eList = eList.OrderBy(A => A.edgePA.X).ThenBy(B => B.edgePB.X).ThenBy(C => C.edgePA.Y).ThenBy(D => D.edgePB.Y).ToList();
            return eList;
        }

        //參考演算法筆記Andrew's Monotone Chain
        public List<Edge> getConvexHull(List<PointF> pList){ //取得Convex Hull的edge
            List<PointF> sort_pList = getSortPoints(pList);
            int count = sort_pList.Count; //所有點的數量
            PointF[] upper_points = new PointF[count]; //Convex Hull上凸包
            PointF[] lower_points = new PointF[count]; //Convex Hull下凸包
            
            int l = 0, u = 0; //Convex Hull上下凸包個數
            for(int i = 0; i<count;i++){
                while(l >= 2 && getCrossProduct(lower_points[l-2], lower_points[l-1], sort_pList[i]) <= 0)
                    l--;
                while(u >= 2 && getCrossProduct(upper_points[u-2], upper_points[u-1], sort_pList[i]) >= 0)
                    u--;
                lower_points[l++] = pList[i];
                upper_points[u++] = pList[i];
            }

            List<Edge> convex_eList = new List<Edge>();
            for(int i=0;i<l-1;i++)
                convex_eList.Add(new Edge(lower_points[i], lower_points[i+1]));
            for(int i=0; i<u-1;i++)
                convex_eList.Add(new Edge(upper_points[i], upper_points[i+1]));
            return convex_eList;
        }

        public List<Edge> getConvexUpper(List<PointF> pList){ //取得Convex Hull的上凸包
            List<PointF> sort_pList = getSortPoints(pList);
            int count = sort_pList.Count; //所有點的數量
            PointF[] upper_points = new PointF[count]; //Convex Hull上凸包
            
            int l = 0, u = 0; //Convex Hull上下凸包個數
            for(int i = 0; i<count;i++){
                while(u >= 2 && getCrossProduct(upper_points[u-2], upper_points[u-1], sort_pList[i]) >= 0)
                    u--;
                upper_points[u++] = pList[i];
            }

            List<Edge> convex_eList = new List<Edge>();
            for(int i=0; i<u-1;i++)
                convex_eList.Add(new Edge(upper_points[i], upper_points[i+1]));
            return convex_eList;
        }

        public List<Edge> getConvexLower(List<PointF> pList){ //取得Convex Hull的上凸包
            List<PointF> sort_pList = getSortPoints(pList);
            int count = sort_pList.Count; //所有點的數量
            PointF[] lower_points = new PointF[count]; //Convex Hull下凸包
            
            int l = 0, u = 0; //Convex Hull上下凸包個數
            for(int i = 0; i<count;i++){
                while(l >= 2 && getCrossProduct(lower_points[l-2], lower_points[l-1], sort_pList[i]) <= 0)
                    l--;
                lower_points[l++] = pList[i];
            }

            List<Edge> convex_eList = new List<Edge>();
            for(int i=0;i<l-1;i++)
                convex_eList.Add(new Edge(lower_points[i], lower_points[i+1]));
            return convex_eList;
        }

        public Edge getPointDiffSideEdge(List<Edge> ch_edges, List<PointF> points){ //只要檢查出一點在左，一點不在左即可
            int edge_idx = -1;
            for(int i = 0;i<ch_edges.Count;i++){
                bool isPAFind = false, isPBFind = false;
                foreach(PointF point in points){
                    if(ch_edges[i].edgePA.Equals(point)){ //edgePA在左側
                        isPAFind = true;
                        break;
                    }
                }

                foreach(PointF point in points){
                    if(ch_edges[i].edgePB.Equals(point)){ //edgePB在左側
                        isPBFind = true;
                        break;
                    }
                }

                if((isPAFind&&!isPBFind) || (!isPAFind&&isPBFind)){ //一點有找到，一點沒找到
                    edge_idx = i;
                    break;
                }
            }
            return ch_edges[edge_idx];
        }

        public List<PointF> getCounterClockwiseSortPoints(List<PointF> pList){ //取得逆時鐘排好的點
            List<PointF> pList_sort = new List<PointF>(pList); //複製List
            PointF center = getCenterPoint(pList);

            for(int i=0;i<pList_sort.Count;i++){ //利用bubble sort排序
                for(int j=0;j<pList_sort.Count-i-1;j++){
                    if(getCrossProduct(center, pList_sort[j], pList_sort[j+1])>0){
                        PointF tmp = pList_sort[j];
                        pList_sort[j] = pList_sort[j+1];
                        pList_sort[j+1] = tmp;
                    }
                }
            }
            return pList_sort;
        }
        
        public PointF getVector(PointF point1, PointF point2){ //取得向量
            return new PointF(point2.X - point1.X, point2.Y - point1.Y);
        }
        public PointF getVerticalVec(PointF vector){ //取得輸入向量對應的垂直向量
            //兩向量若垂直內積會是0
            //vector(x,y) vertical vector(-y,x) -> x*(-y)+y*x = 0 方向依照負號在的位置不同
            return new PointF(-vector.Y, vector.X);
        }
        public float getInnerProduct(PointF vec1, PointF vec2){
            return vec1.X*vec2.X + vec1.Y*vec2.Y;
        }
        public float getCrossProduct(PointF center, PointF point1, PointF point2){ //差積，若小於0表示point1對於point2是順時鐘
            return (point1.X-center.X)*(point2.Y-center.Y) - (point1.Y-center.Y)*(point2.X-center.X);
        }
        public Edge getEdge(PointF point1, PointF point2){ //取得兩點之中垂線
            PointF mid = getCenterPoint(point1, point2); //取得兩點之中點
            //取得兩方向之垂直向量
            PointF verticalVec1 = getVerticalVec(getVector(point1, point2)); //取得兩方向之垂直向量
            PointF verticalVec2 = getVerticalVec(getVector(point2, point1));
            return new Edge(addPoints(mid, multPoints(verticalVec1, max_number)), addPoints(mid, multPoints(verticalVec2, max_number)), point1, point2);
        }
        public PointF getCenterPoint(PointF point1, PointF point2){ //算中心點
            float x = (point1.X + point2.X)/2;
            float y = (point1.Y + point2.Y)/2;
            return new PointF(x, y);
        }

        public PointF getCenterPoint(List<PointF> pList){ //算中心點
            float x = 0;
            float y = 0;
            foreach(PointF point in pList){
                x += point.X;
                y += point.Y;
            }
            return new PointF(x/pList.Count, y/pList.Count);
        }

        public bool is3ALine(PointF point1, PointF point2, PointF point3){ //檢查是否三點共線
            //若三點共線，1-2線段 和 2-3線段的法向量 的內積為0
            if (getInnerProduct(getVector(point1, point2), getVerticalVec(getVector(point2, point3))) == 0)
                return true;
            else return false;
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
        public PointF getIntersection(Edge edge1, Edge edge2){ //找兩線交點
            PointF a = new PointF(edge1.edgePB.X-edge1.edgePA.X, edge1.edgePB.Y-edge1.edgePA.Y);
            PointF b = new PointF(edge2.edgePB.X-edge2.edgePA.X, edge2.edgePB.Y-edge2.edgePA.Y);
            PointF s = new PointF(edge2.edgePA.X-edge1.edgePA.X, edge2.edgePA.Y-edge1.edgePA.Y);
            float cross_sb = s.X*b.Y - s.Y*b.X;
            float cross_ab = a.X*b.Y - a.Y*b.X;
            a.X = a.X*cross_sb/cross_ab;
            a.Y = a.Y*cross_sb/cross_ab;
            return new PointF(edge1.edgePA.X+a.X, edge1.edgePA.Y+a.Y);
        }
        public float getPointDistance(PointF point1, PointF point2){ //運算兩點距離
            return (float)Math.Sqrt(Math.Pow(point1.X-point2.X,2)+Math.Pow(point1.Y-point2.Y,2));
        }

        public PointF addPoints(PointF point1, PointF point2){
            return new PointF(point1.X+point2.X, point1.Y+point2.Y);
        }
        public PointF multPoints(PointF point1, float times){
            return new PointF(point1.X*times, point1.Y*times);
        }
    }
}

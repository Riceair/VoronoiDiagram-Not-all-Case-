using System;
using System.Collections.Generic;
using System.Drawing;

namespace VoronoiDiagram
{
    public class PointEdgeRecoder: Object
    {
        private List<PointF> points_list = new List<PointF>();
        private List<Edge> edges_list = new List<Edge>();

        public PointEdgeRecoder(List<PointF> pList, List<Edge> eList){
            points_list = pList;
            edges_list = eList;
        }

        public PointEdgeRecoder(List<PointF> pList){
            points_list = pList;
        }

        public List<PointF> getPoints(){
            return points_list;
        }

        public List<Edge> getEdges(){
            return edges_list;
        }
    }

    public class Edge: Object
    {
        public List<PointF> edge_points = new List<PointF>(); //紀錄邊的兩點
        public List<PointF> points = new List<PointF>(); //製作該edge的兩點

        public Edge(List<PointF> edge_points, List<PointF> points){
            this.edge_points = edge_points;
            this.points = points;
        }
    }
}

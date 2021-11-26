using System;
using System.Collections.Generic;
using System.Drawing;

namespace VoronoiDiagram
{
    public class PointEdgeRecoder: Object
    {
        public List<PointF> points_list = new List<PointF>();
        public List<Edge> edges_list = new List<Edge>();

        public PointEdgeRecoder(List<PointF> pList, List<Edge> eList){
            points_list = pList;
            edges_list = eList;
        }

        public PointEdgeRecoder(List<PointF> pList){
            points_list = pList;
        }
    }

    public class Edge: Object
    {
        public PointF edgePA, edgePB; //紀錄邊的兩點
        public PointF pointA, pointB; //製作該edge的兩點

        public Edge(PointF edgePA, PointF edgePB, PointF pointA, PointF pointB){
            this.edgePA = edgePA;
            this.edgePB = edgePB;
            this.pointA = pointA;
            this.pointB = pointB;
        }

        public Edge(PointF edgePA, PointF edgePB){
            this.edgePA = edgePA;
            this.edgePB = edgePB;
        }
    }
}

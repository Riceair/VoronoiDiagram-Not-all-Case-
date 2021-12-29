/* $LAN=C#$ */
/*
	Name: PointEdgeRecoder.cs
	Copyright: Copyright © 2021
	Author: 韓定紘 Ding-Hong, Han
    Student ID: M103040011
    Class: 資訊碩一
*/
using System;
using System.Collections.Generic;
using System.Drawing;

namespace VoronoiDiagram
{
    public class PointEdgeRecoder: Object
    {
        public List<PointF> points_list = new List<PointF>();
        public List<Edge> edges_list = new List<Edge>();
        public List<Edge> convex_list = new List<Edge>();
        public List<Edge> hyper_list = new List<Edge>();
        public Color point_color = Color.DarkGray;
        public Color edge_color = Color.Gray;
        public Color convex_color = Color.SkyBlue;
        public Color hyper_color = Color.SeaGreen;

        public PointEdgeRecoder(){}

        public PointEdgeRecoder(List<PointF> pList, List<Edge> eList){
            points_list = pList;
            edges_list = eList;
        }

        public PointEdgeRecoder(List<PointF> pList){
            points_list = pList;
        }

        public Object getClone(){
            return this.MemberwiseClone();
        }
    }

    public class Edge: Object
    {
        public PointF edgePA, edgePB; //紀錄邊的兩點
        public PointF pointA, pointB; //製作該edge的兩點

        public Edge(){}
        public Edge(PointF edgePA, PointF edgePB, PointF pointA, PointF pointB){
            this.edgePA = edgePA;
            this.edgePB = edgePB;
            this.pointA = pointA;
            this.pointB = pointB;
            sortEdgeByY();
            sortPointByY();
        }

        public Edge(PointF edgePA, PointF edgePB){
            this.edgePA = edgePA;
            this.edgePB = edgePB;
            sortEdgeByY();
        }

        public void setPoints(PointF pointA, PointF pointB){ //設置製作edge的兩點
            this.pointA = pointA;
            this.pointB = pointB;
            sortPointByY();
        }

        public void sortEdgeByY(){
            if(edgePA.Y < edgePB.Y){
                PointF temp = edgePA;
                edgePA = edgePB;
                edgePB = temp;
            }
            else if(edgePA.Y == edgePB.Y){
                if(edgePA.X > edgePB.X){
                    PointF temp = edgePA;
                    edgePA = edgePB;
                    edgePB = temp;
                }
            }
        }

        public void sortPointByY(){ //製作edge的兩點要以Y軸大小存入A B
            if(pointA == null || pointB == null) return;

            if(this.pointA.Y < this.pointB.Y){
                PointF temp = this.pointA;
                this.pointA = this.pointB;
                this.pointB = temp;
            }
            else if(this.pointA.Y == this.pointB.Y){
                if(this.pointA.X > this.pointB.X){
                    PointF temp = this.pointA;
                    this.pointA = this.pointB;
                    this.pointB = temp;
                }
            }
        }

        public Object getClone(){
            return this.MemberwiseClone();
        }
    }

    public class ModifyEdge: Object //儲存要修改的edge資訊
    {
        public Edge edge = new Edge();
        public PointF intersection = new PointF();

        public ModifyEdge(Edge edge, PointF intersection){
            this.edge = edge;
            this.intersection = intersection;
        }
    }
}

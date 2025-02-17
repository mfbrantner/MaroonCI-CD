﻿using System;
using System.Collections.Generic;
using Maroon.UI.Charts;
using UnityEngine;
using XCharts;
using System.Linq;

namespace Maroon.Physics.CathodeRayTube
{
    public enum DistanceEnum
    {
        Both,
        Vertical,
        Horizontal
    }

    public enum OrderEnum
    {
        VerticalHorizontal,
        HorizontalVertical,
        Vertical,
        Horizontal
    }

    public enum XAxisEnum
    {
        X,
        Time
    }

    public enum YAxisEnum
    {
        X,
        Vx,
        Fx,
        Y,
        Vy,
        Fy,
        Z,
        Vz,
        Fz
    }

    public class CRTController : MonoBehaviour
    {
        private ElectronLineController _electronLineController;
        [SerializeField] private GameObject screen;
        [SerializeField] private GameObject cathode;
        [SerializeField] private GameObject anode;
        [SerializeField] private GameObject verticalDeflectionPlate;
        [SerializeField] private GameObject horizontalDeflectionPlate;
        [SerializeField] private QuantityInt vX;
        [SerializeField] private QuantityInt vY;
        [SerializeField] private QuantityInt vZ;
        [SerializeField] private QuantityFloat d;
        [SerializeField] private SimpleLineChart plot;

        [SerializeField] private QuantityString fXInfo;
        [SerializeField] private QuantityString fYInfo;
        [SerializeField] private QuantityString fZInfo;
        [SerializeField] private QuantityString eXInfo;
        [SerializeField] private QuantityString eYInfo;
        [SerializeField] private QuantityString eZInfo;

        private int _order;
        public int Order
        {
            get => _order;
            set
            {
                _order = value;
                UpdateOrder();
            }
        }

        public int Distance { get; set; }
        public int XAxis { get; set; }
        public int YAxis { get; set; }

        public const float ElectronCharge = -1.6022e-19f;
        public const float ElectronMass = 9.11e-31f;
        private float _electronGunLength;
        public int lineResolution = 500;

        private GameObject _verticalDeflectionPlateTop;
        private GameObject _verticalDeflectionPlateBottom;
        private GameObject _horizontalDeflectionPlateLeft;
        private GameObject _horizontalDeflectionPlateRight;

        private Vector3 _verticalDeflectionPlateStartPos;
        private Vector3 _horizontalDeflectionPlateStartPos;

        private float _vertDeflPlateDistance;
        private float _horizDeflPlateDistance;

        private List<Vector3> _pointData = new List<Vector3>();
        private List<Vector3> _velocityData = new List<Vector3>();
        private List<Vector3> _forceData = new List<Vector3>();

        public float EX;
        public float EY;
        public float EZ;

        internal float ElectronGunLength { get => _electronGunLength; }
        internal float VerticalPlatePosition { get => verticalDeflectionPlate.transform.position.x - GetCRTStart().x; }
        internal float VerticalPlateLength { get => _verticalDeflectionPlateTop.GetComponent<Renderer>().bounds.size.x / 2; }
        internal float VerticalPlateWidth { get => _verticalDeflectionPlateTop.GetComponent<Renderer>().bounds.size.z / 2; }

        internal float HorizontalPlatePosition { get => horizontalDeflectionPlate.transform.position.x - GetCRTStart().x; }
        internal float HorizontalPlateLength { get => _horizontalDeflectionPlateLeft.GetComponent<Renderer>().bounds.size.x / 2; }
        internal float HorizontalPlateWidth { get => _horizontalDeflectionPlateLeft.GetComponent<Renderer>().bounds.size.y / 2; }

        internal bool VerticalPlateEnabled { get => _verticalDeflectionPlateBottom.activeSelf; }
        internal bool HorizontalPlateEnabled { get => _horizontalDeflectionPlateLeft.activeSelf; }
        internal Vector3 WorldSpaceOrigin { get => GetCRTStart(); }


        private void Start()
        {
            _electronLineController = gameObject.GetComponentInChildren<ElectronLineController>();
            _electronGunLength = anode.transform.position.x - GetCRTStart().x;
            _vertDeflPlateDistance = d;
            _horizDeflPlateDistance = d;

            _verticalDeflectionPlateTop = verticalDeflectionPlate.transform.GetChild(0).gameObject;
            _verticalDeflectionPlateBottom = verticalDeflectionPlate.transform.GetChild(1).gameObject;
            _horizontalDeflectionPlateLeft = horizontalDeflectionPlate.transform.GetChild(0).gameObject;
            _horizontalDeflectionPlateRight = horizontalDeflectionPlate.transform.GetChild(1).gameObject;

            _verticalDeflectionPlateStartPos = verticalDeflectionPlate.transform.position;
            _horizontalDeflectionPlateStartPos = horizontalDeflectionPlate.transform.position;

            for (int i = 0; i < lineResolution; i++)
            {
                _pointData.Add(Vector3.zero);
                _velocityData.Add(Vector3.zero);
                _forceData.Add(Vector3.zero);
            }

            XAxis = (int)XAxisEnum.X;
            YAxis = (int)YAxisEnum.Y;
            
            setPlatePositions();
            UpdateInformation();
        }

        private void FixedUpdate()
        {
            UpdateInformation();
        }

        public void UpdateDistance()
        {
            if (!SimulationController.Instance.SimulationRunning)
                return;
            
            switch ((DistanceEnum)Distance)
            {
                case DistanceEnum.Both:
                    _vertDeflPlateDistance = d;
                    _horizDeflPlateDistance = d;
                    break;
                case DistanceEnum.Vertical:
                    _vertDeflPlateDistance = d;
                    break;
                case DistanceEnum.Horizontal:
                    _horizDeflPlateDistance = d;
                    break;
                default:
                    _vertDeflPlateDistance = d;
                    _horizDeflPlateDistance = d;
                    break;
            }

            setPlatePositions();            
            _electronLineController.UpdateElectronLine();
        }

        public void UpdateOrder()
        {
            if (!SimulationController.Instance.SimulationRunning)
                return;
            
            _horizontalDeflectionPlateLeft.SetActive(true);
            _horizontalDeflectionPlateRight.SetActive(true);
            _verticalDeflectionPlateTop.SetActive(true);
            _verticalDeflectionPlateBottom.SetActive(true);

            Vector3 position;
            switch ((OrderEnum)Order)
            {
                case OrderEnum.HorizontalVertical:
                    verticalDeflectionPlate.transform.position = _horizontalDeflectionPlateStartPos;
                    horizontalDeflectionPlate.transform.position = _verticalDeflectionPlateStartPos;
                    break;
                
                case OrderEnum.VerticalHorizontal:
                    verticalDeflectionPlate.transform.position = _verticalDeflectionPlateStartPos;
                    horizontalDeflectionPlate.transform.position = _horizontalDeflectionPlateStartPos;
                    break;

                case OrderEnum.Horizontal:
                    position = _verticalDeflectionPlateStartPos;
                    position.x += (_horizontalDeflectionPlateStartPos.x - _verticalDeflectionPlateStartPos.x) / 2;
                    horizontalDeflectionPlate.transform.position = position;
                    verticalDeflectionPlate.transform.position = Vector3.zero;
                    _verticalDeflectionPlateTop.SetActive(false);
                    _verticalDeflectionPlateBottom.SetActive(false);
                    break;
                
                case OrderEnum.Vertical:
                    position = _verticalDeflectionPlateStartPos;
                    position.x += (_horizontalDeflectionPlateStartPos.x - _verticalDeflectionPlateStartPos.x) / 2;
                    verticalDeflectionPlate.transform.position = position;
                    horizontalDeflectionPlate.transform.position = Vector3.zero;
                    _horizontalDeflectionPlateLeft.SetActive(false);
                    _horizontalDeflectionPlateRight.SetActive(false);
                    break;

                default:
                    verticalDeflectionPlate.transform.position = _verticalDeflectionPlateStartPos;
                    horizontalDeflectionPlate.transform.position = _horizontalDeflectionPlateStartPos;
                    break;
            }

            setPlatePositions();
            _electronLineController.UpdateElectronLine();
        }

        private void setPlatePositions()
        {
            var newPosition = verticalDeflectionPlate.transform.position;
            _verticalDeflectionPlateTop.transform.position = newPosition + new Vector3(0, _vertDeflPlateDistance / 2, 0);
            _verticalDeflectionPlateBottom.transform.position = newPosition - new Vector3(0, _vertDeflPlateDistance / 2, 0);
            newPosition = horizontalDeflectionPlate.transform.position;
            _horizontalDeflectionPlateRight.transform.position = newPosition + new Vector3(0, 0, _horizDeflPlateDistance / 2);
            _horizontalDeflectionPlateLeft.transform.position = newPosition - new Vector3(0, 0, _horizDeflPlateDistance / 2);
        }

        private void UpdateInformation()
        {
            int informationResolution = 1000;
            float length;
            float size;

            EX = (int)(vX / (Math.Truncate(informationResolution * _electronGunLength) / informationResolution));
            EY = (int)(vY / (Math.Truncate(informationResolution * _vertDeflPlateDistance) / informationResolution));
            EZ = (int)(vZ / (Math.Truncate(informationResolution * _horizDeflPlateDistance) / informationResolution));

            eXInfo.Value = vX.Value + " / " +
                           (Math.Truncate(informationResolution * _electronGunLength) / informationResolution)
                           + " \n= " + EX + " V/m";
            eYInfo.Value = vY.Value + " / " +
                           (Math.Truncate(informationResolution * _vertDeflPlateDistance) / informationResolution)
                           + " \n= " + EY + " V/m";
            eZInfo.Value = vZ.Value + " / " +
                           (Math.Truncate(informationResolution * _horizDeflPlateDistance) / informationResolution)
                           + " \n= " + EZ + " V/m";

            float fXValue = -ElectronCharge *
                       (vX / (float)(Math.Truncate(informationResolution * _electronGunLength) /
                                     informationResolution)) * (float)Math.Pow(10, 15);
            float fYValue = -ElectronCharge *
                            (vY / (float)(Math.Truncate(informationResolution * _vertDeflPlateDistance) /
                                          informationResolution)) * (float)Math.Pow(10, 15);
            float fZValue = -ElectronCharge *
                            (vZ / (float)(Math.Truncate(informationResolution * _horizDeflPlateDistance) /
                                          informationResolution)) * (float)Math.Pow(10, 15);

            fXInfo.Value = "1.6022e-19 * " + EX + " * H(" +
                           (Math.Truncate(informationResolution * _electronGunLength) / informationResolution) +
                           " - x)" + " \n= " + fXValue + " * (10 ^ -15) N"  ;

            size = _verticalDeflectionPlateTop.GetComponent<Renderer>().bounds.size.z / 2;
            length = Math.Abs(GetCRTStart().x - verticalDeflectionPlate.transform.position.x);
            fYInfo.Value = "1.6022e-19 * " + EY + " * \nH(" +
                           (Math.Truncate(informationResolution * size) / informationResolution)
                           + " - abs(x - " + (Math.Truncate(informationResolution * length) / informationResolution) +
                           "))" + " * \nH(" + _verticalDeflectionPlateTop.GetComponent<Renderer>().bounds.size.z / 2 
                           + " - abs(z - " + GetCRTStart().z + "))"
                           + " \n= " + fYValue + " * (10 ^ -15) N";

            size = _horizontalDeflectionPlateLeft.GetComponent<Renderer>().bounds.size.y / 2;
            length = Math.Abs(GetCRTStart().x - horizontalDeflectionPlate.transform.position.x);
            fZInfo.Value = "1.6022e-19 * " + EZ + " * \nH(" +
                           (Math.Truncate(informationResolution * size) / informationResolution)
                           + " - abs(x - " + (Math.Truncate(informationResolution * length) / informationResolution) +
                           "))" + " * \nH(" + _horizontalDeflectionPlateLeft.GetComponent<Renderer>().bounds.size.y / 2 
                           + " - abs(y - " + GetCRTStart().y + "))"
                           + " \n= " + fZValue + " * (10 ^ -15) N";
        }

        public void UpdateData(List<Vector3> points, List<Vector3> velocities, List<Vector3> forces)
        {
            _pointData = points;
            _velocityData = velocities;
            _forceData = forces;
        }

        public void UpdatePlot()
        {
            plot.ResetObject();
            float timeStep = GetTimeStep();
            var lineChart = plot.GetComponent<LineChart>();
            List<float> xAxisData = new List<float>();
            List<float> yAxisData = new List<float>();

            lineChart.xAxis0.minMaxType = Axis.AxisMinMaxType.Default;
            lineChart.yAxis0.minMaxType = Axis.AxisMinMaxType.Default;

            switch ((XAxisEnum)XAxis)
            {
                case XAxisEnum.X:
                    lineChart.xAxis0.minMaxType = Axis.AxisMinMaxType.Custom;
                    lineChart.xAxis0.min = 0;
                    lineChart.xAxis0.max = _pointData.Last().x;
                    xAxisData.AddRange(_pointData.Select(point => point.x));
                    break;
                case XAxisEnum.Time:
                    lineChart.xAxis0.minMaxType = Axis.AxisMinMaxType.Custom;
                    lineChart.xAxis0.min = 0;
                    lineChart.xAxis0.max = _pointData.Count * timeStep;
                    for (int i = 0; i < _pointData.Count; i++)
                        xAxisData.Add(i * timeStep);
                    break;
            }

            switch ((YAxisEnum)YAxis)
            {
                case YAxisEnum.X:
                    lineChart.yAxis0.minMaxType = Axis.AxisMinMaxType.Custom;
                    lineChart.yAxis0.min = 0;
                    lineChart.yAxis0.max = _pointData.Last().x;
                    yAxisData.AddRange(_pointData.Select(point => point.x));
                    break;
                case YAxisEnum.Vx:
                    yAxisData.AddRange(_velocityData.Select(point => point.x));
                    break;
                case YAxisEnum.Fx:
                    lineChart.yAxis0.minMaxType = Axis.AxisMinMaxType.Custom;
                    lineChart.yAxis0.min = 7 * -(float)Math.Pow(10, -15);
                    lineChart.yAxis0.max = 7 * (float)Math.Pow(10, -15);
                    yAxisData.AddRange(_forceData.Select(point => point.x));
                    break;
                case YAxisEnum.Y:
                    lineChart.yAxis0.minMaxType = Axis.AxisMinMaxType.Custom;
                    lineChart.yAxis0.min = 0;
                    lineChart.yAxis0.max = _pointData.Last().y;
                    yAxisData.AddRange(_pointData.Select(point => point.y));
                    break;
                case YAxisEnum.Vy:
                    yAxisData.AddRange(_velocityData.Select(point => point.y));
                    break;
                case YAxisEnum.Fy:
                    lineChart.yAxis0.minMaxType = Axis.AxisMinMaxType.Custom;
                    lineChart.yAxis0.min = 7 * -(float)Math.Pow(10, -15);
                    lineChart.yAxis0.max = 7 * (float)Math.Pow(10, -15);
                    yAxisData.AddRange(_forceData.Select(point => point.y));
                    break;
                case YAxisEnum.Z:
                    lineChart.yAxis0.minMaxType = Axis.AxisMinMaxType.Custom;
                    lineChart.yAxis0.min = 0;
                    lineChart.yAxis0.max = _pointData.Last().z;
                    yAxisData.AddRange(_pointData.Select(point => point.z));
                    break;
                case YAxisEnum.Vz:
                    yAxisData.AddRange(_velocityData.Select(point => point.z));
                    break;
                case YAxisEnum.Fz:
                    lineChart.yAxis0.minMaxType = Axis.AxisMinMaxType.Custom;
                    lineChart.yAxis0.min = 7 * -(float)Math.Pow(10, -15);
                    lineChart.yAxis0.max = 7* (float)Math.Pow(10, -15);
                    yAxisData.AddRange(_forceData.Select(point => point.z));
                    break;
            }

            plot.AddData(xAxisData.Zip(yAxisData, (x, y) => Tuple.Create(x, y)).ToList());
        }

        public float GetTimeStep()
        {
            float v = (float)Math.Sqrt(-2 * ElectronCharge * vX / ElectronMass);
            float t = (float)Math.Sqrt(2 * _electronGunLength * ElectronMass /
                                       (ElectronCharge * (-vX / _electronGunLength)));
            t += GetCRTDist() / v;
            return t / lineResolution;
        }

        public float GetCRTDist()
        {
            return screen.transform.position.x - GetCRTStart().x;
        }

        public Vector3 GetCRTStart()
        {
            var point = cathode.transform.position;
            point.x += cathode.GetComponent<Renderer>().bounds.size.x / 2;
            return point;
        }
    }
}
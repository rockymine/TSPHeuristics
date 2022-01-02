using Excubo.Blazor.Canvas;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Algorithms;
using TravellingSalesmanProblem.Graph;
using WebApplication.Components;

namespace WebApplication.Utils {
    public class AppState {
        public GraphProblem Instance { get; set; }

        private float _scale = 30;
        public float Scale {
            get => _scale;
            set {
                if (value == _scale)
                    return;
                if (value < 0 || value > 40)
                    return;

                _scale = value;
            }
        }

        private float _nodeRadius = 8;
        public float NodeRadius {
            get => _nodeRadius;
            set {
                if (value == _nodeRadius)
                    return;
                if (value < 1 || value > 10)
                    return;

                _nodeRadius = value;
            }
        }

        private int _animationDelay = 1;
        public int AnimationDelay {
            get => _animationDelay;
            set {
                if (value == _animationDelay)
                    return;
                if (value < 1 || value > 5000)
                    return;

                _animationDelay = value;
            }
        }

        private int _maxX = 30;
        public int MaxX {
            get => _maxX;
            set {
                if (value == _maxX)
                    return;
                if (value < 10 || value > 90)
                    return;

                _maxX = value;
            }
        }

        private int _maxY = 30;
        public int MaxY {
            get => _maxY;
            set {
                if (value == _maxY)
                    return;
                if (value < 10 || value > 90)
                    return;

                _maxY = value;
            }
        }

        private int _nodeCount = 30;
        public int NodeCount {
            get => _nodeCount;
            set {
                if (value == _nodeCount)
                    return;
                if (value < 5 || value > 90)
                    return;

                _nodeCount = value;
            }
        }

        private int _startNode;
        public int StartNode {
            get => _startNode;
            set {
                if (value == _startNode)
                    return;
                if (value < 0 || value > Instance.Nodes.Count - 1)
                    return;

                _startNode = value;
            }
        }

        private int _phaseLength = 1;
        public int PhaseLength {
            get => _phaseLength;
            set {
                if (value == _phaseLength)
                    return;
                if (value < 1 || value > 80)
                    return;

                _phaseLength = value;
            }
        }

        private int _maxPhases = 100;
        public int MaxPhases {
            get => _maxPhases;
            set {
                if (value == _maxPhases)
                    return;
                if (value < 100 || value > 1000)
                    return;

                _maxPhases = value;
            }
        }

        private double _startTemp = 100;
        public double StartTemp {
            get => _startTemp;
            set {
                if (value == _startTemp)
                    return;
                if (value < 50 || value > 100_000) {
                    return;
                }

                _startTemp = value;
            }
        }

        private double _minTemp = Math.Pow(10, -8);
        public double MinTemp {
            get => _minTemp;
            set {
                if (value == _minTemp)
                    return;
                if (value < 0.0000001 || value >= _startTemp)
                    return;

                _minTemp = value;
            }
        }

        private double _alphaSA = 0.98;
        public double AlphaSA {
            get => _alphaSA;
            set {
                if (value == _alphaSA)
                    return;
                if (value < 0.5 || value >= 1)
                    return;

                _alphaSA = value;
            }
        }

        private int _iterations = 10;
        public int Iterations {
            get => _iterations;
            set {
                if (value == _iterations)
                    return;
                if (value <= 0 || value > 50)
                    return;

                _iterations = value;
            }
        }

        private double _alphaACS = 0.1;
        public double AlphaACS {
            get => _alphaACS;
            set {
                if (value == _alphaACS)
                    return;
                if (value <= 0 || value >= 1)
                    return;

                _alphaACS = value;
            }
        }

        private double _beta = 2;
        public double Beta {
            get => _beta;
            set {
                if (value == _beta)
                    return;
                if (value <= 0 || value >= 10)
                    return;

                _beta = value;
            }
        }

        private int _antCount = 10;
        public int AntCount {
            get => _antCount;
            set {
                if (value == _antCount)
                    return;
                if (value < 1 || value > Instance.Nodes.Count)
                    return;

                _antCount = value;
            }
        }

        public LinkedList<GraphState> GraphHistory;
        public LinkedListNode<GraphState> CurrentHistoryNode;

        public Canvas PreviousCanvas;
        public Canvas CurrentCanvas;

        public TableGenerator TableGenerator;
        public ChartJS[] ChartJS = new ChartJS[5];

        //private GraphProblem Instance;
        public string Error;

        public AlgorithmEnum AlgorithmEnum;
        public NeighbourType NeighbourEnum;
        public GraphSource GraphSource;
        public DescentType DescentType;
        public NearestNeighbour NearestNeighbor = new();
        public SimulatedAnnealing SimulatedAnnealing = new();
        public AntSystem AntSystem = new();
        public HillClimbing HillClimbing = new();

        public FileInput FileInput = new();
        public string DefaultGraph = @"n,1,11,5
n,2,6,4
n,3,4,10
n,4,4,2
n,5,2,4
n,6,7,7
n,7,8,8
n,8,9,2
n,9,5,7
n,10,7,1
n,11,1,6
n,12,11,11";

        public bool AutoAdvance;
        public bool AnnotateEdges;
        public bool ColorizeChanges = true;
        public string BackgroundColor = "#FFFFFF";
        public bool MultiStart;
        public bool ShowGrid = true;
        public CanvasSettings CanvasSettings = new();

        public Stopwatch Stopwatch = new();
        public TimeSpan TimeSpan = new();

        public bool BreakWhenPathsAreEqual = true;
    }
}
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
                /* Scale 0 collapses CanvasSettings.Width/Height to zero, leaving an invisible
                 * canvas that cannot be recovered from without reloading. */
                if (value < 1 || value > 40)
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

        /* The old ceiling of 90 nodes was set by what the app could survive, not by the heuristics.
         * Measured in the browser after the fixes, from the click on Execute until the page answers
         * again: Nearest Neighbour 2.9 s at 300 nodes, Simulated Annealing with phase length 10
         * 7.2 s, Hill Climbing with 2-opt and steepest descent 12.7 s, with swap 36.6 s. The
         * coordinate range follows so a graph that large still has room to spread out; the canvas
         * caps its own pixel size, so a wide range no longer means an enormous buffer. */
        private const int MaxNodeCount = 300;
        private const int MaxCoordinate = 300;

        private int _maxX = 30;
        public int MaxX {
            get => _maxX;
            set {
                if (value == _maxX)
                    return;
                if (value < 10 || value > MaxCoordinate)
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
                if (value < 10 || value > MaxCoordinate)
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
                if (value < 5 || value > MaxNodeCount)
                    return;

                _nodeCount = value;
            }
        }

        private int NodeCountOfInstance => Instance?.Nodes.Count ?? 0;

        private int _startNode;
        /* The setter dereferenced Instance, which is null until a graph has been executed - and
         * the Nearest Neighbour panel holding this control is what the app shows on load, so
         * touching the start node first thing threw NullReferenceException. The getter clamps as
         * well, because a start node picked for a large graph stayed behind when a smaller graph
         * was loaded afterwards and then indexed Instance.Nodes out of range. */
        public int StartNode {
            get {
                var count = NodeCountOfInstance;
                return count == 0 ? 0 : Math.Clamp(_startNode, 0, count - 1);
            }
            set {
                if (value < 0)
                    return;

                var count = NodeCountOfInstance;
                if (count > 0 && value > count - 1)
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

        /* Every Ant System iteration keeps its own pheromone snapshot in the history, one Edge per
         * edge of the complete graph - n*(n-1)/2 of them at roughly 95 bytes each. That, not the
         * runtime, is what a long run runs out of: 300 nodes at 500 iterations would ask for over
         * two gigabytes. The iteration count is therefore capped against the loaded instance,
         * which leaves 500 for anything up to about 130 nodes and 58 at 300. */
        private const int MaxIterations = 500;
        private const long IterationMemoryBudget = 250L * 1024 * 1024;
        private const int BytesPerEdge = 95;

        private int IterationLimit {
            get {
                var count = NodeCountOfInstance;
                if (count < 2)
                    return MaxIterations;

                var perIteration = (long)count * (count - 1) / 2 * BytesPerEdge;
                return (int)Math.Clamp(IterationMemoryBudget / perIteration, 1, MaxIterations);
            }
        }

        private int _iterations = 50;
        public int Iterations {
            get => Math.Clamp(_iterations, 1, IterationLimit);
            /* Snaps to the ceiling instead of refusing the input: an entry that is silently
             * discarded leaves the panel showing the old number with no hint of where the limit
             * is, while snapping shows it. */
            set {
                if (value < 1)
                    return;

                _iterations = Math.Min(value, IterationLimit);
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

        /* The colony used to be capped at one ant per node, and under the greedy transition rule
         * that was the only sensible cap: two ants leaving the same node walked the same tour, so
         * the extra ant cost time and found nothing. With the random proportional rule two ants
         * from the same start diverge, and more ants than nodes are worth having. The cost is
         * linear and carries no memory of its own - 0.0045 s per ant at 100 nodes, flat from 25
         * ants upwards - so the cap is now a plain runtime ceiling. */
        private const int MaxAntCount = 500;

        private int _antCount = 25;
        /* Same two problems as StartNode: the value is read before any graph exists, and a colony
         * sized for one instance stayed behind when another was loaded. */
        public int AntCount {
            get => Math.Clamp(_antCount, 1, MaxAntCount);
            set {
                if (value < 1)
                    return;

                _antCount = Math.Min(value, MaxAntCount);
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
        public GlobalUpdatingRule GlobalUpdatingRule;
        public SelectionRule SelectionRule = SelectionRule.Proportional;
        public NearestNeighbour NearestNeighbor = new();
        public SimulatedAnnealing SimulatedAnnealing = new();
        public AntSystem AntSystem = new();
        public HillClimbing HillClimbing = new();

        public FileInput FileInput = new();
        public FileOutput FileOutput = new();
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
        public bool CalculateTemperature;
        public bool ShowGrid = true;
        public CanvasSettings CanvasSettings = new();

        public Stopwatch Stopwatch = new();
        public TimeSpan TimeSpan = new();

        public bool BreakWhenPathsAreEqual = true;
    }
}
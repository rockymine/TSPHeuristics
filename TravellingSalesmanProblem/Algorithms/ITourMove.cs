using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    /* One concrete move on one concrete tour. A move is bound to the tour it was built on,
     * because that is what its delta is measured against. Selection and application are separate:
     * NeighbourState chooses a move, the move applies itself, so a caller can also name the
     * positions itself. Delta is exact so its sign agrees with the tour costs the searches
     * compare it with; only Describe() rounds, and only for display. */
    public interface ITourMove {
        NeighbourType Type { get; }
        double Delta { get; }
        MoveInfo Describe();
        GraphProblem Apply();
    }
}

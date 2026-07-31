using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    /* One concrete move on one concrete tour.
     *
     * Selecting a move and applying it used to be the same call - NeighbourState.Create picked the
     * positions according to the descent type and returned the resulting tour, so there was no way
     * to say "apply this move at these positions". Three-opt and the double bridge did not even
     * have a type: their positions were drawn, scored and applied inside a single static method.
     *
     * A move is bound to the tour it was built on, because that is what its delta is measured
     * against. Delta is exact so its sign agrees with the tour costs the searches compare it with;
     * only Describe() rounds, and only for display. */
    public interface ITourMove {
        NeighbourType Type { get; }
        double Delta { get; }
        MoveInfo Describe();
        GraphProblem Apply();
    }
}

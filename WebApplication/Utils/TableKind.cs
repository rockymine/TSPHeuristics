namespace WebApplication.Utils {
    /* Which table a TableGenerator builds. Auto derives it from the algorithm, which is all a run
     * showing one table needs; the manual mode shows the move history and the distance matrix side
     * by side and has to name the second one explicitly. */
    public enum TableKind {
        Auto = 0,
        DistanceMatrix = 1
    }
}

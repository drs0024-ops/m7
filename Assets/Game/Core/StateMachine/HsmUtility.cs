namespace Game.Core.StateMachine
{
    public static class HsmUtility
    {
        public static State Lca(State a, State b)
        {
            int depthA = 0, depthB = 0;
            for (var s = a; s != null; s = s.Parent) depthA++;
            for (var s = b; s != null; s = s.Parent) depthB++;

            while (depthA > depthB) { a = a.Parent; depthA--; }
            while (depthB > depthA) { b = b.Parent; depthB--; }

            while (a != b)
            {
                a = a.Parent;
                b = b.Parent;
            }
            return a;
        }
    }
}   
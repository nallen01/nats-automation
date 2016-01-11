using System;

namespace NatsAutomation
{
    class Match
    {
        public static String toName(int n_type, int n_round, int n_num)
        {
            if ((n_type < 0) || (n_round < 0) || (n_num < 0))
                return null;
            String str = "";
            if (n_type == 1)
                return "P " + n_num;
            if (n_type == 2)
                return "Q " + n_num;
            if (n_type == 5)
                return "F " + n_num;
            if (n_type == 3)
                str = "QF ";
            if (n_type == 4)
                str = "SF ";
            return str + n_round + "-" + n_num;
        }
    }
}

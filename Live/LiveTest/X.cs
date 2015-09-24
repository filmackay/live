namespace Vertigo.Live.Test
{
    public class X
    {
        public Live<int?> I = Live<int?>.NewDefault();
        public Live<int> In = Live<int>.NewDefault();
        public Live<string> S = Live<string>.NewDefault();
        public Live<X> Sub = Live<X>.NewDefault();
    }
}
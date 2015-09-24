namespace Vertigo.Live
{
    public interface ILiveMutable<in TIState>
        where TIState : IState
    {
        bool Set(TIState state);
    }
}
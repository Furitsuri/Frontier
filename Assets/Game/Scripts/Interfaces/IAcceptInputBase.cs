/// <summary>
///  非ジェネリックの入力受付基底インターフェースです
///  ※はじめは入力受付をbool, Direction, Vector2などで分けていたため、
///  それらの入力受付へ付加するインターフェースとして作成しましたが、
///  現在は入力をすべてInputContextに集約しているため、不要かもしれません( 2026/02/22 )
/// </summary>
public interface IAcceptInputBase
{
    bool Accept( InputContext context );
}
using System;

public static class LazyInject
{
    public static T GetOrCreate<T>( ref T field, Func<T> createFunc ) where T : class
    {
        if( null == field )
        {
            field = createFunc();
            NullCheck.AssertNotNull( field, nameof( field ) );
        }
        return field;
    }
}
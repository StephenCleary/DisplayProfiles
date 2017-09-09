using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace DisplayProfilesGui
{
    public sealed class Exceptional<T>
    {
        public Exceptional(T value) => ObjectValue = value;
        public Exceptional(Exception value) => ObjectValue = value;

        public Exceptional(Func<T> value)
        {
            try
            {
                ObjectValue = value();
            }
            catch (Exception ex)
            {
                ObjectValue = ex;
            }
        }

        public override string ToString() => ObjectValue.ToString();

        public object ObjectValue { get; }

        public T Value
        {
            get
            {
                var ex = Exception;
                if (ex != null)
                    ExceptionDispatchInfo.Capture(ex).Throw();
                return (T)ObjectValue;
            }
        }

        public T TryValue
        {
            get
            {
                if (Exception != null)
                    return default(T);
                return (T)ObjectValue;
            }
        }

        public Exception Exception => ObjectValue as Exception;
    }

    public static class Exceptional
    {
        public static Exceptional<T> Create<T>(Func<T> valueFunc) => new Exceptional<T>(valueFunc);
    }
}

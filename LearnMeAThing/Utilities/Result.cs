using System;

namespace LearnMeAThing.Utilities
{
    readonly struct Result
    {
        public static readonly Result Fail = new Result(false);
        public static readonly Result Succeed = new Result(true);

        public readonly bool Success;

        public Result(bool success)
        {
            Success = success;
        }

        public static Result<T> From<T>(T value) => new Result<T>(true, value);
        public static Result<T> FailFor<T>() => new Result<T>(false, default);
    }

    readonly struct Result<T>
    {
        public readonly bool Success;
        private readonly T _Value;
        public T Value
        {
            get
            {
                if (!Success) throw new InvalidOperationException($"Tried to access {nameof(Value)} on an unsuccessful {nameof(Result<T>)}");

                return _Value;
            }
        }

        public Result(bool success, T value)
        {
            Success = success;
            _Value = value;
        }
    }
}

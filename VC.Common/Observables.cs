using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VC
{
    public static class ObservableArithmetic
    {
        public static IObservable<double> Const(this double constant)
        {
            return Observable.Return(constant);
        }
        public static IObservable<double> Add(this IObservable<double> left, IObservable<double> right)
        {
            return left.CombineLatest(right, (l, r) => l + r);
        }
        public static IObservable<double> Subtract(this IObservable<double> left, IObservable<double> right)
        {
            return left.CombineLatest(right, (l, r) => l - r);
        }
        public static IObservable<double> Multiply(this IObservable<double> left, IObservable<double> right)
        {
            return left.CombineLatest(right, (l, r) => l * r);
        }
        public static IObservable<double> Divide(this IObservable<double> left, IObservable<double> right)
        {
            return left.CombineLatest(right, (l, r) => l / r);
        }
    }
}

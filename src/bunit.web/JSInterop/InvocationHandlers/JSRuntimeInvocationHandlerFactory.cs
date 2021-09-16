using System;
using System.Threading.Tasks;

namespace Bunit.JSInterop.InvocationHandlers
{
	internal class JSRuntimeInvocationHandlerFactory<TResult, TException> : JSRuntimeInvocationHandler<TResult>
		where TException : Exception
	{
		private readonly Func<JSRuntimeInvocation, TResult>? resultFactory;
		private readonly Func<JSRuntimeInvocation, TException>? exceptionFactory;

		public JSRuntimeInvocationHandlerFactory(Func<JSRuntimeInvocation, TResult>? rFactory, Func<JSRuntimeInvocation, TException>? eFactory, InvocationMatcher matcher, bool isCatchAllHandler) : base(matcher, isCatchAllHandler)
		{
			resultFactory = rFactory;
			exceptionFactory = eFactory;
		}

		protected override internal Task<TResult> HandleAsync(JSRuntimeInvocation invocation)
		{
			if (exceptionFactory != null && exceptionFactory.Invoke(invocation) is TException t)
			{
				base.SetException<TException>(t);
			}
			else if (resultFactory != null && resultFactory.Invoke(invocation) is TResult res)
			{
				base.SetResult(res);
			}
			return base.HandleAsync(invocation);
		}
	}
}
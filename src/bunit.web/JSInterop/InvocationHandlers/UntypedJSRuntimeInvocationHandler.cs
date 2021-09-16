using System;
using System.Threading.Tasks;

namespace Bunit.JSInterop.InvocationHandlers
{
	/// <summary>
	/// Represents a handler for an invocation of a JavaScript function with specific arguments
	/// that can return any type, depending on the SetResult/SetCancelled/SetException invocations.
	/// </summary>
	public class UntypedJSRuntimeInvocationHandler : JSRuntimeInvocationHandler
	{
		private readonly InvocationMatcher invocationMatcher;

		private readonly BunitJSInterop jsInterop;

		private JSRuntimeInvocation? currentInvocation;

		/// <summary>
		/// Initializes a new instance of the <see cref="UntypedJSRuntimeInvocationHandler"/> class.
		/// </summary>
		/// <param name="interop">The bUnit JSInterop to setup the invocation handling with.</param>
		/// <param name="matcher">An invocation matcher used to determine if the handler should handle an invocation.</param>
		/// <param name="isCatchAllHandler">Set to true if this handler is a catch all handler, that should only be used if there are no other non-catch all handlers available.</param>
		public UntypedJSRuntimeInvocationHandler(BunitJSInterop interop, InvocationMatcher matcher, bool isCatchAllHandler = true)
			: base(matcher, isCatchAllHandler)
		{
			jsInterop = interop;
			invocationMatcher = matcher ?? throw new ArgumentNullException(nameof(matcher));

			jsInterop.SetGenericInvocationHandler(this);
		}

		/// <summary>
		/// Sets the result factory function, that when invoked sets the <typeparamref name="TReturnType"/> result that invocations will receive.
		/// </summary>
		/// <param name="resultFactory">The result factory function that creates the handler result.</param>
		/// <returns>This handler to allow calls to be chained.</returns>
		public JSRuntimeInvocationHandler SetResult<TReturnType>(Func<JSRuntimeInvocation, TReturnType> resultFactory)
		{
			if (resultFactory == null)
			{
				throw new ArgumentNullException(nameof(resultFactory));
			}

			if (currentInvocation != null && resultFactory.Invoke(currentInvocation.Value) is TReturnType res)
			{
				base.SetResultBase(res);
			}
			else
			{
				var handler = new JSRuntimeInvocationHandlerFactory<TReturnType, Exception>(resultFactory, null, invocationMatcher, IsCatchAllHandler);

				jsInterop.AddInvocationHandler<TReturnType>(handler);
				jsInterop.SetGenericInvocationHandler(null);
			}

			return this;
		}

		/// <summary>
		/// Sets the exception factory, that when invoked determines the <typeparamref name="TException"/> exception that invocations will receive.
		/// </summary>
		/// <param name="exceptionFactory">The exception function factory to set.</param>
		/// <returns>This handler to allow calls to be chained.</returns>
		public JSRuntimeInvocationHandler SetException<TReturnType, TException>(Func<JSRuntimeInvocation, TException> exceptionFactory) where TException : Exception
		{
			if (exceptionFactory == null)
			{
				throw new ArgumentNullException(nameof(exceptionFactory));
			}

			if (currentInvocation != null && exceptionFactory.Invoke(currentInvocation.Value) is TException excp)
			{
				base.SetException(excp);
			}
			else
			{
				var handler = new JSRuntimeInvocationHandlerFactory<TReturnType, TException>(null, exceptionFactory, invocationMatcher, IsCatchAllHandler);

				jsInterop.AddInvocationHandler<TReturnType>(handler);
				jsInterop.SetGenericInvocationHandler(null);
			}

			return this;
		}

		/// <summary>
		/// Call this to have the this handler handle the <paramref name="invocation"/>.
		/// </summary>
		/// <param name="invocation">Invocation to handle.</param>
		protected override internal Task<object> HandleAsync(JSRuntimeInvocation invocation)
		{
			currentInvocation = invocation;
			return base.HandleAsync(invocation);
		}
	}
}
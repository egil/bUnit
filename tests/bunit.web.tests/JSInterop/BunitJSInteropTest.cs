using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Shouldly;
using Xunit;

namespace Bunit.JSInterop
{
	public partial class BunitJSInteropTest
	{
		private static BunitJSInterop CreateSut(JSRuntimeMode mode) => new BunitJSInterop { Mode = mode };

		[Fact(DisplayName = "Mock returns default value in loose mode without invocation setup")]
		public async Task Test001()
		{
			var sut = CreateSut(JSRuntimeMode.Loose);

			var result = await sut.JSRuntime.InvokeAsync<object>("ident", Array.Empty<object>());

			result.ShouldBe(default);
		}

		[Fact(DisplayName = "After invocation a invocation should be visible from the Invocations list")]
		public void Test002()
		{
			var identifier = "fooFunc";
			var args = new[] { "bar", "baz" };
			using var cts = new CancellationTokenSource();
			var sut = CreateSut(JSRuntimeMode.Loose);

			sut.JSRuntime.InvokeAsync<object>(identifier, cts.Token, args);

			var invocation = sut.Invocations[identifier].Single();
			invocation.Identifier.ShouldBe(identifier);
			invocation.Arguments.ShouldBe(args);
			invocation.CancellationToken.ShouldBe(cts.Token);
		}

		[Fact(DisplayName = "Mock throws exception when in strict mode and invocation has not been setup")]
		public void Test003()
		{
			var sut = CreateSut(JSRuntimeMode.Strict);
			var identifier = "func";
			var args = new[] { "bar", "baz" };

			Should.Throw<JSRuntimeUnhandledInvocationException>(async () => await sut.JSRuntime.InvokeVoidAsync(identifier, args))
				.Invocation.ShouldSatisfyAllConditions(
					x => x.Identifier.ShouldBe(identifier),
					x => x.Arguments.ShouldBe(args));

			Should.Throw<JSRuntimeUnhandledInvocationException>(async () => await sut.JSRuntime.InvokeAsync<object>(identifier, args))
				.Invocation.ShouldSatisfyAllConditions(
					x => x.Identifier.ShouldBe(identifier),
					x => x.Arguments.ShouldBe(args));
		}

		[Fact(DisplayName = "All invocations received AFTER a invocation handler " +
							"has a result set, receives the same result")]
		public async Task Test005x()
		{
			var identifier = "func";
			var expectedResult = Guid.NewGuid();
			var sut = CreateSut(JSRuntimeMode.Strict);
			var jsRuntime = sut.JSRuntime;
			var handler = sut.Setup<Guid>(identifier);

			handler.SetResult(expectedResult);

			var i1 = jsRuntime.InvokeAsync<Guid>(identifier);
			var i2 = jsRuntime.InvokeAsync<Guid>(identifier);

			(await i1).ShouldBe(expectedResult);
			(await i2).ShouldBe(expectedResult);
		}

		[Fact(DisplayName = "All invocations received BEFORE a invocation handler " +
							"has a result set, receives the same result")]
		public async Task Test005()
		{
			var identifier = "func";
			var expectedResult = Guid.NewGuid();
			var sut = CreateSut(JSRuntimeMode.Strict);
			var jsRuntime = sut.JSRuntime;
			var handler = sut.Setup<Guid>(identifier);

			var i1 = jsRuntime.InvokeAsync<Guid>(identifier);
			var i2 = jsRuntime.InvokeAsync<Guid>(identifier);

			handler.SetResult(expectedResult);

			(await i1).ShouldBe(expectedResult);
			(await i2).ShouldBe(expectedResult);
		}

		[Fact(DisplayName = "Invocations receive the latest result set in a invocation handler")]
		public async Task Test006x()
		{
			var identifier = "func";
			var sut = CreateSut(JSRuntimeMode.Strict);
			var handler = sut.Setup<Guid>(identifier);
			var jsRuntime = sut.JSRuntime;

			var expectedResult1 = Guid.NewGuid();
			handler.SetResult(expectedResult1);
			var i1 = jsRuntime.InvokeAsync<Guid>(identifier);

			var expectedResult2 = Guid.NewGuid();
			handler.SetResult(expectedResult2);
			var i2 = jsRuntime.InvokeAsync<Guid>(identifier);

			(await i1).ShouldBe(expectedResult1);
			(await i2).ShouldBe(expectedResult2);
		}

		[Fact(DisplayName = "A invocation handler can be canceled for any waiting received invocations")]
		public void Test007()
		{
			var identifier = "func";
			var sut = CreateSut(JSRuntimeMode.Strict);
			var handler = sut.Setup<Guid>(identifier);
			var invocation = sut.JSRuntime.InvokeAsync<Guid>(identifier);

			handler.SetCanceled();

			invocation.IsCanceled.ShouldBeTrue();
		}

		[Fact(DisplayName = "A invocation handler can be canceled after it has been set to a different result")]
		public void Test107()
		{
			var identifier = "func";
			var sut = CreateSut(JSRuntimeMode.Strict);
			var handler = sut.Setup<Guid>(identifier);

			var invocation1 = sut.JSRuntime.InvokeAsync<Guid>(identifier);
			handler.SetResult(Guid.NewGuid());
			invocation1.IsCompletedSuccessfully.ShouldBeTrue();

			handler.SetCanceled();
			var invocation2 = sut.JSRuntime.InvokeAsync<Guid>(identifier);
			invocation2.IsCanceled.ShouldBeTrue();
		}

		[Fact(DisplayName = "A invocation handler can throw an exception for any waiting received invocations")]
		public async Task Test008()
		{
			var identifier = "func";
			var sut = CreateSut(JSRuntimeMode.Strict);
			var handler = sut.Setup<Guid>(identifier);
			var invocation = sut.JSRuntime.InvokeAsync<Guid>(identifier);
			var expectedException = new InvalidOperationException("TADA");

			handler.SetException(expectedException);

			var actual = await Should.ThrowAsync<InvalidOperationException>(invocation.AsTask());
			actual.ShouldBe(expectedException);
			invocation.IsFaulted.ShouldBeTrue();
		}

		[Fact(DisplayName = "A invocation handler can throw an exception after it has been set to a different result")]
		public void Test108()
		{
			var identifier = "func";
			var sut = CreateSut(JSRuntimeMode.Strict);
			var handler = sut.Setup<Guid>(identifier);
			var expectedException = new InvalidOperationException("TADA");

			var invocation1 = sut.JSRuntime.InvokeAsync<Guid>(identifier);
			handler.SetResult(Guid.NewGuid());
			invocation1.IsCompletedSuccessfully.ShouldBeTrue();

			handler.SetException(expectedException);
			var invocation2 = sut.JSRuntime.InvokeAsync<Guid>(identifier);
			invocation2.IsFaulted.ShouldBeTrue();
		}

		[Fact(DisplayName = "Invocations returns all from a invocation handler")]
		public void Test009()
		{
			var identifier = "func";
			var sut = new BunitJSInterop();
			var handler = sut.Setup<Guid>(identifier, _ => true);
			sut.JSRuntime.InvokeAsync<Guid>(identifier, "first");
			sut.JSRuntime.InvokeAsync<Guid>(identifier, "second");

			var invocations = handler.Invocations;

			invocations[identifier].Count.ShouldBe(2);
			invocations[identifier][0].Arguments[0].ShouldBe("first");
			invocations[identifier][1].Arguments[0].ShouldBe("second");
		}

		[Fact(DisplayName = "Arguments used in Setup are matched with invocations")]
		public void Test010()
		{
			var sut = CreateSut(JSRuntimeMode.Strict);
			var planned = sut.Setup<object>("foo", "bar", 42);

			sut.JSRuntime.InvokeAsync<object>("foo", "bar", 42);

			Should.Throw<JSRuntimeUnhandledInvocationException>(() => sut.JSRuntime.InvokeAsync<object>("foo", "bar", 41));

			planned.Invocations.Count.ShouldBe(1);
			var invocation = planned.Invocations["foo"][0];
			invocation.Identifier.ShouldBe("foo");
			invocation.Arguments[0].ShouldBe("bar");
			invocation.Arguments[1].ShouldBe(42);
		}

		[Fact(DisplayName = "Argument matcher used in Setup are matched with invocations")]
		public void Test011()
		{
			var sut = CreateSut(JSRuntimeMode.Strict);
			var planned = sut.Setup<object>("foo", x => x.Arguments.Count == 1);

			sut.JSRuntime.InvokeAsync<object>("foo", 42);

			Should.Throw<JSRuntimeUnhandledInvocationException>(() => sut.JSRuntime.InvokeAsync<object>("foo", "bar", 42));

			planned.Invocations.Count.ShouldBe(1);
			var invocation = planned.Invocations["foo"][0];
			invocation.Identifier.ShouldBe("foo");
			invocation.Arguments.Count.ShouldBe(1);
			invocation.Arguments[0].ShouldBe(42);
		}

		[Fact(DisplayName = "SetupVoid returns a invocation handler that does not take a result object")]
		public async Task Test012()
		{
			var identifier = "func";
			var sut = CreateSut(JSRuntimeMode.Strict);
			var handler = sut.SetupVoid(identifier);

			var invocation = sut.JSRuntime.InvokeVoidAsync(identifier);
			handler.SetVoidResult();

			await invocation;

			invocation.IsCompletedSuccessfully.ShouldBeTrue();
		}

		[Fact(DisplayName = "Arguments used in SetupVoid are matched with invocations")]
		public void Test013()
		{
			var sut = CreateSut(JSRuntimeMode.Strict);
			var planned = sut.SetupVoid("foo", "bar", 42);

			sut.JSRuntime.InvokeVoidAsync("foo", "bar", 42);

			Should.Throw<JSRuntimeUnhandledInvocationException>(async () => await sut.JSRuntime.InvokeVoidAsync("foo", "bar", 41));

			planned.Invocations.Count.ShouldBe(1);
			var invocation = planned.Invocations["foo"][0];
			invocation.Identifier.ShouldBe("foo");
			invocation.Arguments[0].ShouldBe("bar");
			invocation.Arguments[1].ShouldBe(42);
		}

		[Fact(DisplayName = "Argument matcher used in SetupVoid are matched with invocations")]
		public void Test014()
		{
			var sut = CreateSut(JSRuntimeMode.Strict);
			var planned = sut.SetupVoid("foo", x => x.Arguments.Count == 2);
			sut.JSRuntime.InvokeVoidAsync("foo", "bar", 42);

			Should.Throw<JSRuntimeUnhandledInvocationException>(async () => await sut.JSRuntime.InvokeVoidAsync("foo", 42));
			Should.Throw<JSRuntimeUnhandledInvocationException>(async () => await sut.JSRuntime.InvokeVoidAsync("foo"));

			planned.Invocations.Count.ShouldBe(1);
			var invocation = planned.Invocations["foo"][0];
			invocation.Identifier.ShouldBe("foo");
			invocation.Arguments.Count.ShouldBe(2);
			invocation.Arguments[0].ShouldBe("bar");
			invocation.Arguments[1].ShouldBe(42);
		}

		[Fact(DisplayName = "Empty Setup returns the same result for all matching return type invocation")]
		public async Task Test015()
		{
			var sut = CreateSut(JSRuntimeMode.Strict);
			var handler = sut.Setup<Guid>();
			var jsRuntime = sut.JSRuntime;

			var expectedResult1 = Guid.NewGuid();
			handler.SetResult(expectedResult1);
			var i1 = jsRuntime.InvokeAsync<Guid>("someFunc");

			var i2 = jsRuntime.InvokeAsync<Guid>("otherFunc");

			(await i1).ShouldBe(expectedResult1);
			(await i2).ShouldBe(expectedResult1);
		}

		[Fact(DisplayName = "Empty Setup only matches the configured return type")]
		public void Test016()
		{
			var sut = CreateSut(JSRuntimeMode.Strict);
			var planned = sut.Setup<Guid>();

			Should.Throw<JSRuntimeUnhandledInvocationException>(() => sut.JSRuntime.InvokeAsync<string>("foo"));

			planned.Invocations.Count.ShouldBe(0);
		}

		[Fact(DisplayName = "Empty Setup allows to return different results by return types")]
		public async Task Test017()
		{
			var sut = CreateSut(JSRuntimeMode.Strict);
			var handler1 = sut.Setup<Guid>();
			var handler2 = sut.Setup<string>();
			var jsRuntime = sut.JSRuntime;

			var expectedResult1 = Guid.NewGuid();
			handler1.SetResult(expectedResult1);
			var i1 = jsRuntime.InvokeAsync<Guid>("someFunc");

			var expectedResult2 = "somestring";
			handler2.SetResult(expectedResult2);
			var i2 = jsRuntime.InvokeAsync<string>("otherFunc");

			(await i1).ShouldBe(expectedResult1);
			(await i2).ShouldBe(expectedResult2);
		}

		[Fact(DisplayName = "Empty Setup is only used when there is no handler exist for the invocation identifier")]
		public async Task Test018()
		{
			var sut = CreateSut(JSRuntimeMode.Strict);
			var catchAllhandler = sut.Setup<Guid>();
			var jsRuntime = sut.JSRuntime;

			var catchAllexpectedResult = Guid.NewGuid();
			catchAllhandler.SetResult(catchAllexpectedResult);

			var expectedResult = Guid.NewGuid();
			var handler = sut.Setup<Guid>("func");
			handler.SetResult(expectedResult);

			var i1 = jsRuntime.InvokeAsync<Guid>("someFunc");

			var i2 = jsRuntime.InvokeAsync<Guid>("func");

			(await i1).ShouldBe(catchAllexpectedResult);
			(await i2).ShouldBe(expectedResult);
		}

		[Fact(DisplayName = "Empty Setup uses the last set result")]
		public async Task Test019()
		{
			var sut = CreateSut(JSRuntimeMode.Strict);
			var handler1 = sut.Setup<Guid>();
			var handler2 = sut.Setup<Guid>();
			var jsRuntime = sut.JSRuntime;

			var expectedResult1 = Guid.NewGuid();
			var expectedResult2 = Guid.NewGuid();

			handler1.SetResult(expectedResult1);
			handler2.SetResult(expectedResult2);

			var i1 = jsRuntime.InvokeAsync<Guid>("someFunc");

			(await i1).ShouldBe(expectedResult2);
		}

		[Fact(DisplayName = "SetupVoid matches all void invocations")]
		public async Task Test020()
		{
			var identifier = "someFunc";
			var sut = CreateSut(JSRuntimeMode.Strict);
			var handler = sut.SetupVoid();

			Should.Throw<JSRuntimeUnhandledInvocationException>(() => sut.JSRuntime.InvokeAsync<string>(identifier));

			var invocation = sut.JSRuntime.InvokeVoidAsync(identifier);
			handler.SetVoidResult();

			await invocation;

			invocation.IsCompletedSuccessfully.ShouldBeTrue();
			handler.Invocations.Count.ShouldBe(1);
		}

		[Fact(DisplayName = "Empty Setup is not used for invocation with void return types")]
		public void Test021()
		{
			var sut = CreateSut(JSRuntimeMode.Strict);

			sut.Setup<Guid>();

			Should.Throw<JSRuntimeUnhandledInvocationException>(async () => await sut.JSRuntime.InvokeVoidAsync("someFunc"));
		}

		[Fact(DisplayName = "CatchAll handlers SetupVoid is only used when there is no void handler")]
		public async Task Test022()
		{
			var identifier = "someFunc";
			var sut = CreateSut(JSRuntimeMode.Strict);
			var handler = sut.SetupVoid(identifier);
			var plannedCatchall = sut.SetupVoid(); // should not be used, even if registered after more specific handler

			var invocation = sut.JSRuntime.InvokeVoidAsync(identifier);
			handler.SetVoidResult();

			handler.Invocations.Count.ShouldBe(1);
			plannedCatchall.Invocations.Count.ShouldBe(0);

			await invocation;
			invocation.IsCompletedSuccessfully.ShouldBeTrue();
		}

		[Fact(DisplayName = "The last handler matching an invocation receives the invocation")]
		public void Test030()
		{
			var identifier = "someFunc";
			var sut = CreateSut(JSRuntimeMode.Strict);
			var h1 = sut.Setup<string>(identifier);
			var h2 = sut.Setup<string>(identifier);

			sut.JSRuntime.InvokeAsync<string>(identifier);

			h1.Invocations.ShouldBeEmpty();
			h2.Invocations.Count.ShouldBe(1);
		}

		[Fact(DisplayName = "TryGetInvokeHandler returns null when no handlers matches the arguments")]
		public void Test040()
		{
			var sut = CreateSut(JSRuntimeMode.Loose);

			var actual = sut.TryGetInvokeHandler<string>("foo");

			actual.ShouldBeNull();
		}

		[Fact(DisplayName = "TryGetInvokeHandler returns the last handler matching the input parameters")]
		public void Test041()
		{
			var sut = CreateSut(JSRuntimeMode.Loose);
			var h1 = sut.Setup<string>("foo");
			var expected = sut.Setup<string>("foo");

			var actual = sut.TryGetInvokeHandler<string>("foo");

			actual.ShouldBe(expected);
			actual.ShouldNotBe(h1);
		}

		[Fact(DisplayName = "TryGetInvokeVoidHandler can find void-return handlers")]
		public void Test042()
		{
			var sut = CreateSut(JSRuntimeMode.Loose);
			sut.Setup<object>("foo");
			var expected = sut.SetupVoid("foo");

			var actual = sut.TryGetInvokeVoidHandler("foo");

			actual.ShouldBe(expected);
		}

		[Fact(DisplayName = "Mock returns default value from IJSInProcessRuntime's invoke method in loose mode without invocation setup")]
		public void Test043()
		{
			var sut = CreateSut(JSRuntimeMode.Loose);

			var result = ((IJSInProcessRuntime)sut.JSRuntime).Invoke<object>("ident", Array.Empty<object>());

			result.ShouldBe(default);
		}

		[Fact(DisplayName = "After IJSInProcessRuntime invocation a invocation should be visible from the Invocations list")]
		public void Test044()
		{
			var identifier = "fooFunc";
			var args = new[] { "bar", "baz" };
			var sut = CreateSut(JSRuntimeMode.Loose);

			((IJSInProcessRuntime)sut.JSRuntime).Invoke<object>(identifier, args);

			var invocation = sut.Invocations[identifier].Single();
			invocation.Identifier.ShouldBe(identifier);
			invocation.Arguments.ShouldBe(args);
		}

		[Fact(DisplayName = "IJSInProcessRuntime invocations receive the result set in a planned invocation")]
		public void Test045()
		{
			var identifier = "func";
			var args = new[] { "bar", "baz" };
			var sut = CreateSut(JSRuntimeMode.Strict);

			var expectedResult = Guid.NewGuid();
			var planned = sut.Setup<Guid>(identifier, args);
			planned.SetResult(expectedResult);

			var i = ((IJSInProcessRuntime)sut.JSRuntime).Invoke<Guid>(identifier, args);

			i.ShouldBe(expectedResult);
		}

		[Fact(DisplayName = "Mock throws exception when in strict mode and IJSInProcessRuntime invocation has not been setup")]
		public void Test046()
		{
			var sut = CreateSut(JSRuntimeMode.Strict);
			var identifier = "func";
			var args = new[] { "bar", "baz" };

			var exception = Should.Throw<JSRuntimeUnhandledInvocationException>(() => ((IJSInProcessRuntime)sut.JSRuntime).Invoke<object>(identifier, args));
			exception.Invocation.Identifier.ShouldBe(identifier);
			exception.Invocation.Arguments.ShouldBe(args);
		}

		[Fact(DisplayName = "Untyped setup can return a typed result from a factory function")]
		public async Task Test059()
		{
			var sut = CreateSut(JSRuntimeMode.Strict);
			var identifier = "func";

			var jsRuntime = sut.JSRuntime;

			var handler = sut.Setup(i => i.Identifier == identifier);
			handler.SetResult(_ => false);

			var i1 = await jsRuntime.InvokeAsync<bool>(identifier);
			i1.ShouldBe(false);
		}

		[Fact(DisplayName = "Untyped setup can throw an exception from a factory function")]
		public void Test060()
		{
			var sut = CreateSut(JSRuntimeMode.Strict);
			var identifier = "func";

			var jsRuntime = sut.JSRuntime;

			var handler = sut.Setup(i => i.Identifier == identifier);
			handler.SetException<bool, NotImplementedException>(_ => throw new NotImplementedException());

			Should.Throw<NotImplementedException>(async () => await jsRuntime.InvokeAsync<bool>(identifier));
		}

		[Fact(DisplayName = "An untyped invocation handler can be canceled")]
		public void Test061()
		{
			var sut = CreateSut(JSRuntimeMode.Strict);
			var identifier = "func";

			var jsRuntime = sut.JSRuntime;

			var handler = sut.Setup(i => i.Identifier == identifier);
			handler.SetCanceled();

			var res = jsRuntime.InvokeAsync<bool>(identifier);
			res.IsCanceled.ShouldBeTrue();
		}

		[Fact(DisplayName = "Untyped supports setting result after invocation")]
		public async Task Test062()
		{
			var sut = CreateSut(JSRuntimeMode.Strict);
			var identifier = "func";
			var jsRuntime = sut.JSRuntime;

			var handler = sut.Setup(i => i.Identifier == identifier);

			var i1 = jsRuntime.InvokeAsync<bool>(identifier);

			handler.SetResult(_ => false);

			(await i1).ShouldBe(false);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Bunit.TestDoubles.Authorization
{
	/// <summary>
	/// Root authorization service that manages different authentication/authorization state
	/// in the system.
	/// </summary>
	public class TestAuthorizationContext
	{
		private readonly FakeAuthorizationService _authService = new FakeAuthorizationService();
		private readonly FakeAuthorizationPolicyProvider _policyProvider = new FakeAuthorizationPolicyProvider();
		private readonly FakeAuthenticationStateProvider _authProvider = new FakeAuthenticationStateProvider();

		/// <summary>
		/// Gets whether user is authenticated.
		/// </summary>
		public bool IsAuthenticated { get; private set; } = false;

		/// <summary>
		/// Gets the authorization state for the user.
		/// </summary>
		public AuthorizationState Authorization { get; private set; } = AuthorizationState.Unauthorized;

		/// <summary>
		/// Gets the set of roles for the current user.
		/// </summary>
		public IEnumerable<string> Roles { get; private set; } = Array.Empty<string>();

		/// <summary>
		/// Gets a list of the AuthorizeAsync calls that were made to the IAuthorizationService.
		/// Callers can verify authorization request was made as expected from TestAuthorizationContext.
		/// </summary>
		public IEnumerable<(ClaimsPrincipal user, object resource, IEnumerable<IAuthorizationRequirement> requirements)> AuthorizeCalls
		{
			get => _authService.AuthorizeCalls;
		}

		/// <summary>
		/// Registers authorization services with the specified service provider.
		/// </summary>
		/// <param name="serviceProvider">Service provider to use.</param>
		public void RegisterAuthorizationServices(TestServiceProvider serviceProvider)
		{
			serviceProvider.AddSingleton<IAuthorizationService>(_authService);
			serviceProvider.AddSingleton<IAuthorizationPolicyProvider>(_policyProvider);
			serviceProvider.AddSingleton<AuthenticationStateProvider>(_authProvider);
		}

		/// <summary>
		/// Authenticates the user with specified name and authorization state.
		/// </summary>
		/// <param name="userName">User name for the principal identity.</param>
		/// <param name="state">Authorization state.</param>
		/// <param name="roles">Roles for the claims principal.</param>
		public void SetAuthorized(string userName, AuthorizationState state = AuthorizationState.Authorized, IEnumerable<string>? roles = null)
		{
			IsAuthenticated = true;
			Roles = roles ?? Array.Empty<string>();
			_authProvider.TriggerAuthenticationStateChanged(userName, roles);

			Authorization = state;
			_authService.SetAuthorizationState(state);
		}

		/// <summary>
		/// Puts the authorization services into an unauthenticated and unauthorized state.
		/// </summary>
		public void SetNotAuthorized()
		{
			IsAuthenticated = false;
			Roles = Array.Empty<string>();
			_authProvider.TriggerAuthenticationStateChanged();

			Authorization = AuthorizationState.Unauthorized;
			_authService.SetAuthorizationState(AuthorizationState.Unauthorized);
		}

		/// <summary>
		/// Factory method to create a ClaimsPrincipal from a FakePrincipal and its data.
		/// </summary>
		/// <param name="userName">User name for principal identity.</param>
		/// <param name="roles">Roles for the user.</param>
		/// <returns>ClaimsPrincipal created from this data.</returns>
		public static ClaimsPrincipal CreatePrincipal(string userName, IEnumerable<string>? roles = null)
		{
			var r = roles ?? Array.Empty<string>();
			var principal = new ClaimsPrincipal(
				new FakePrincipal { Identity = new FakeIdentity { Name = userName }, Roles = r });
			return principal;
		}
	}
}

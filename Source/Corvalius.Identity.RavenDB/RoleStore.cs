﻿using Microsoft.AspNet.Identity;
using Raven.Abstractions.Exceptions;
using Raven.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Corvalius.Identity.RavenDB
{
    /// <summary>
    /// Creates a new instance of a persistence store for roles.
    /// </summary>
    /// <typeparam name="TRole">The type of the class representing a role</typeparam>
    public class RoleStore<TRole> : RoleStore<TRole, IDocumentStore, string>
        where TRole : IdentityRole<string>
    {
        public RoleStore(IDocumentStore context, IdentityErrorDescriber describer = null) : base(context, describer) { }
    }

    /// <summary>
    /// Creates a new instance of a persistence store for roles.
    /// </summary>
    /// <typeparam name="TRole">The type of the class representing a role.</typeparam>
    /// <typeparam name="TDocumentStore">The type of the data context class used to access the store.</typeparam>
    public class RoleStore<TRole, TDocumentStore> : RoleStore<TRole, TDocumentStore, string>
        where TRole : IdentityRole<string>
        where TDocumentStore : class, IDocumentStore
    {
        public RoleStore(TDocumentStore context, IdentityErrorDescriber describer = null) : base(context, describer) { }
    }

    /// <summary>
    /// Creates a new instance of a persistence store for roles.
    /// </summary>
    /// <typeparam name="TRole">The type of the class representing a role.</typeparam>
    /// <typeparam name="TDocumentStore">The type of the data context class used to access the store.</typeparam>
    /// <typeparam name="TKey">The type of the primary key for a role.</typeparam>
    public class RoleStore<TRole, TDocumentStore, TKey> :
        IQueryableRoleStore<TRole>,
        IRoleClaimStore<TRole>
        where TRole : IdentityRole<TKey>
        where TKey : IEquatable<TKey>
        where TDocumentStore : class, IDocumentStore
    {
        public RoleStore(TDocumentStore context, IdentityErrorDescriber describer = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            Context = context;
            ErrorDescriber = describer ?? new IdentityErrorDescriber();
            _session = new Lazy<IAsyncDocumentSession>(InitializeSession, true);
        }

        private bool _disposed;


        /// <summary>
        /// Gets the database context for this store.
        /// </summary>
        public TDocumentStore Context { get; private set; }

        private Lazy<IAsyncDocumentSession> _session;

        protected IAsyncDocumentSession Session
        {
            get { return _session.Value; }
        }

        private IAsyncDocumentSession InitializeSession()
        {
            var session = Context.OpenAsyncSession();
            session.Advanced.UseOptimisticConcurrency = true;
            return session;
        }


        /// <summary>
        /// Gets or sets the <see cref="IdentityErrorDescriber"/> for any error that occurred with the current operation.
        /// </summary>
        public IdentityErrorDescriber ErrorDescriber { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating if changes should be persisted after CreateAsync, UpdateAsync and DeleteAsync are called.
        /// </summary>
        /// <value>
        /// True if changes should be automatically persisted, otherwise false.
        /// </value>
        public bool DoSaveChanges { get; set; } = true;

        /// <summary>Saves the current store.</summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public Task SaveChanges(CancellationToken cancellationToken, bool forceSave = false)
        {
            return (DoSaveChanges || forceSave) ? Session.SaveChangesAsync(cancellationToken) : Task.FromResult(0);
        }

        /// <summary>
        /// Creates a new role in a store as an asynchronous operation.
        /// </summary>
        /// <param name="role">The role to create in the store.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the <see cref="IdentityResult"/> of the asynchronous query.</returns>
        public async virtual Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
                throw new ArgumentNullException(nameof(role));

            var roleId = IdentityRole<TKey>.CreateId(role.NormalizedName);            
            await Session.StoreAsync(role, roleId, cancellationToken);
            await SaveChanges(cancellationToken);

            return IdentityResult.Success;
        }

        /// <summary>
        /// Updates a role in a store as an asynchronous operation.
        /// </summary>
        /// <param name="role">The role to update in the store.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the <see cref="IdentityResult"/> of the asynchronous query.</returns>
        public async virtual Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
                throw new ArgumentNullException(nameof(role));

            var roleId = IdentityRole<TKey>.CreateId(role.NormalizedName);
            var stored = await Session.LoadAsync<TRole>(roleId);
            var etag = Session.Advanced.GetEtagFor(stored);

            await Session.StoreAsync(role, etag, roleId, cancellationToken);

            try
            {
                await SaveChanges(cancellationToken);
            }
            catch (ConcurrencyException)
            {
                return IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
            }

            return IdentityResult.Success;
        }

        /// <summary>
        /// Deletes a role from the store as an asynchronous operation.
        /// </summary>
        /// <param name="role">The role to delete from the store.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the <see cref="IdentityResult"/> of the asynchronous query.</returns>
        public async virtual Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
                throw new ArgumentNullException(nameof(role));

            Session.Delete(IdentityRole<TKey>.CreateId(role.NormalizedName));

            try
            {
                await SaveChanges(cancellationToken);
            }
            catch (ConcurrencyException)
            {
                return IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
            }

            return IdentityResult.Success;
        }

        /// <summary>
        /// Gets the ID for a role from the store as an asynchronous operation.
        /// </summary>
        /// <param name="role">The role whose ID should be returned.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that contains the ID of the role.</returns>
        public Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
                throw new ArgumentNullException(nameof(role));

            return Task.FromResult(ConvertIdToString(role.Id));
        }

        /// <summary>
        /// Gets the name of a role from the store as an asynchronous operation.
        /// </summary>
        /// <param name="role">The role whose name should be returned.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that contains the name of the role.</returns>
        public Task<string> GetRoleNameAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
                throw new ArgumentNullException(nameof(role));

            return Task.FromResult(role.Name);
        }

        /// <summary>
        /// Sets the name of a role in the store as an asynchronous operation.
        /// </summary>
        /// <param name="role">The role whose name should be set.</param>
        /// <param name="roleName">The name of the role.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public Task SetRoleNameAsync(TRole role, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            role.Name = roleName;
            return Task.FromResult(0);
        }

        /// <summary>
        /// Converts the provided <paramref name="id"/> to a strongly typed key object.
        /// </summary>
        /// <param name="id">The id to convert.</param>
        /// <returns>An instance of <typeparamref name="TKey"/> representing the provided <paramref name="id"/>.</returns>
        public virtual TKey ConvertIdFromString(string id)
        {
            if (id == null)
                return default(TKey);

            return (TKey)TypeDescriptor.GetConverter(typeof(TKey)).ConvertFromInvariantString(id);
        }

        /// <summary>
        /// Converts the provided <paramref name="id"/> to its string representation.
        /// </summary>
        /// <param name="id">The id to convert.</param>
        /// <returns>An <see cref="string"/> representation of the provided <paramref name="id"/>.</returns>
        public virtual string ConvertIdToString(TKey id)
        {
            if (id.Equals(default(TKey)))
                return null;

            return id.ToString();
        }

        /// <summary>
        /// Finds the role who has the specified ID as an asynchronous operation.
        /// </summary>
        /// <param name="id">The role ID to look for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that result of the look up.</returns>
        public async virtual Task<TRole> FindByIdAsync(string id, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            return await Session.LoadAsync<TRole>(id.ToString());
        }

        /// <summary>
        /// Finds the role who has the specified normalized name as an asynchronous operation.
        /// </summary>
        /// <param name="normalizedName">The normalized role name to look for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that result of the look up.</returns>
        public async virtual Task<TRole> FindByNameAsync(string normalizedName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            return await Session.LoadAsync<TRole>(IdentityRole<TKey>.CreateId(normalizedName));
        }

        /// <summary>
        /// Get a role's normalized name as an asynchronous operation.
        /// </summary>
        /// <param name="role">The role whose normalized name should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that contains the name of the role.</returns>
        public virtual Task<string> GetNormalizedRoleNameAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
                throw new ArgumentNullException(nameof(role));

            return Task.FromResult(role.NormalizedName);
        }

        /// <summary>
        /// Set a role's normalized name as an asynchronous operation.
        /// </summary>
        /// <param name="role">The role whose normalized name should be set.</param>
        /// <param name="normalizedName">The normalized name to set</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public virtual Task SetNormalizedRoleNameAsync(TRole role, string normalizedName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (role == null)
                throw new ArgumentNullException(nameof(role));

            role.NormalizedName = normalizedName;
            return Task.FromResult(0);
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        /// <summary>
        /// Dispose the stores
        /// </summary>
        public void Dispose()
        {
            _disposed = true;
        }

        /// <summary>
        /// Get the claims associated with the specified <paramref name="role"/> as an asynchronous operation.
        /// </summary>
        /// <param name="role">The role whose claims should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that contains the claims granted to a role.</returns>
        public async Task<IList<Claim>> GetClaimsAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (role == null)
                throw new ArgumentNullException(nameof(role));

            var stored = await Session.LoadAsync<TRole>(IdentityRole<TKey>.CreateId(role.NormalizedName));
            return new List<Claim>(stored.Claims.Select(c => new Claim(c.ClaimType, c.ClaimValue)));
        }

        /// <summary>
        /// Adds the <paramref name="claim"/> given to the specified <paramref name="role"/>.
        /// </summary>
        /// <param name="role">The role to add the claim to.</param>
        /// <param name="claim">The claim to add to the role.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public async Task AddClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (role == null)
                throw new ArgumentNullException(nameof(role));

            if (claim == null)
                throw new ArgumentNullException(nameof(claim));

            var stored = await Session.LoadAsync<TRole>(IdentityRole<TKey>.CreateId(role.NormalizedName));
            stored.Claims.Add(new IdentityRoleClaim { ClaimType = claim.Type, ClaimValue = claim.Value });

            await SaveChanges(cancellationToken);
        }

        /// <summary>
        /// Removes the <paramref name="claim"/> given from the specified <paramref name="role"/>.
        /// </summary>
        /// <param name="role">The role to remove the claim from.</param>
        /// <param name="claim">The claim to remove from the role.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public async Task RemoveClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (role == null)
                throw new ArgumentNullException(nameof(role));

            if (claim == null)
                throw new ArgumentNullException(nameof(claim));

            var stored = await Session.LoadAsync<TRole>(IdentityRole<TKey>.CreateId(role.NormalizedName));

            var toRemove = new List<IdentityRoleClaim>();            
            foreach (var c in stored.Claims)
            {
                if (c.ClaimValue == claim.Value && c.ClaimType == claim.Type)
                    toRemove.Add(c);
            }

            foreach (var c in toRemove)
                stored.Claims.Remove(c);

            await SaveChanges(cancellationToken);
        }

        public IQueryable<TRole> Roles
        {
            get
            {
                throw new NotSupportedException();
            }
        }
    }
}

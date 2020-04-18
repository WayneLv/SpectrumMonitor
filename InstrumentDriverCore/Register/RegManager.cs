/******************************************************************************
 *                                                                         
 *               
 *               All rights reserved.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using InstrumentDriver.Core.Utility;

namespace InstrumentDriver.Core.Register
{
    /// <summary>
    /// A generic implementation of IRegManager ... no product specific implementation!
    /// </summary>
    public class RegManager : IRegManager
    {
        #region member variables & constructor

        private readonly object mGroupLock = new object();

        /// <summary>
        /// Case-insensitive dictionary for managing groups of registers
        /// </summary>
        private readonly Dictionary <string, IRegister[]> mGroups =
            new Dictionary <string, IRegister[]>( StringComparer.InvariantCultureIgnoreCase );

        private readonly Dictionary <string, IRegisterSet> mRegisterSets =
            new Dictionary <string, IRegisterSet>( StringComparer.InvariantCultureIgnoreCase );

        /// <summary>
        /// Case-insensitive dictionary from managing groups of groups
        /// </summary>
        /// <remarks>
        /// SFP users may desire different grouping of registers than developers create. For example,
        /// developers will likely use RegisterSets to group registers by function but the user may
        /// wish to see all registers on a particular board.
        /// </remarks>
        private readonly Dictionary <string, IEnumerable <string>> mSuperGroups =
            new Dictionary <string, IEnumerable <string>>( StringComparer.InvariantCultureIgnoreCase );

        /// <summary>
        /// Case-insensitive dictionary for managing IDirtyBits (associated with corresponding group in mGroups).
        /// </summary>
        private readonly Dictionary <string, IDirtyBit> mDirtyBits =
            new Dictionary <string, IDirtyBit>( StringComparer.InvariantCultureIgnoreCase );

        private readonly object mLookupRegLock = new object();
        private readonly Dictionary <int, IRegister> mAddressLookupReg = new Dictionary <int, IRegister>();

        private readonly Dictionary <string, IRegister> mNameLookupReg =
            new Dictionary <string, IRegister>( StringComparer.InvariantCultureIgnoreCase );

        private const string WILDCARD = "*";

        /// <summary>
        /// Construct a RegManager with the default name ('Default').  No registers are automatically
        /// created or added.
        /// </summary>
        public RegManager() : this( "Default" )
        {
        }

        /// <summary>
        /// Construct a RegManager with the specified name. No registers are automatically
        /// created or added.
        /// </summary>
        /// <param name="name"></param>
        public RegManager( string name )
        {
            Name = name;
        }

        #endregion member variables & constructor

        #region Implementation of IRegManager

        /// <summary>
        /// Name of register manager (useful for identifying specific instance if a module supports
        /// multiple instances of IRegManager).
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Add a collection of registers under 'GroupName'. If the group has already been
        /// defined this generates an error unless the group ReferenceEquals() the existing
        /// group (in which case this method is a NOP).
        /// 
        /// AddGroup will also create an instance of IDirtyBit and assign it to each register
        /// (all registers in the group share the same instance) which is useful for determining
        /// which groups of registers are dirty and need Apply() called.
        /// 
        /// RegManager assumes that the register array may be sparse (i.e. some of the
        /// array elements may be null).  However, other parts of the system may assume
        /// non-sparse arrays -- be sure to check usage of a group if you are adding a
        /// sparse array.
        /// 
        /// NOTE: if a register is added to the group *after* calling AddGroup() the dirty bit
        ///       needs to be "manually" added to the register.  For example:
        /// 
        ///            IRegister[] group = new IRegister[4];
        ///            mRegManager.AddGroup( "MyGroup", group ); // group[*] is null!!!
        ///            group[0] = new Reg32(...);  // register is in group but no dirty bit!
        ///            group[0].GroupDirtyBit = mRegManager.GetGroupDirtyBit( "MyGroup" );
        /// 
        /// NOTE: if a register belongs to multiple groups, it will only set the dirty bit for one of them!
        /// </summary>
        /// <param name="groupName">Name of group, must be non-null, unique, and not "*" (may be string.Empty)</param>
        /// <param name="group">collection of registers</param>
        /// <exception cref="InvalidParameterException">if the group already exists</exception>
        public virtual void AddGroup( string groupName, IRegister[] group )
        {
            lock( mGroupLock )
            {
                if( mGroups.ContainsKey( groupName ) ||
                    mRegisterSets.ContainsKey( groupName ) ||
                    mSuperGroups.ContainsKey( groupName ) )
                {
                    if( ReferenceEquals( group, mGroups[ groupName ] ) )
                    {
                        // NOP
                        return;
                    }
                    throw new InvalidParameterException( string.Format( "RegManager '{0}' already contains group '{1}'",
                                                                        Name,
                                                                        groupName ) );
                }
                IDirtyBit dirtyBit = new DirtyBit();
                foreach( var register in group )
                {
                    if( register != null )
                    {
                        register.GroupDirtyBit = dirtyBit;
                    }
                }
                mGroups[ groupName ] = group;
                mDirtyBits[ groupName ] = dirtyBit;
                // If we've already created the name lookup cache, refresh it
                if( mNameLookupReg.Count > 0 )
                {
                    UpdateRegisterDictionaries( true );
                }
            }
        }

        /// <summary>
        /// Add a collection of registers contained by the IRegisterSet instance under 'GroupName'.
        /// If the group has already been defined this generates an error.
        /// 
        /// AddGroup will also create an instance of IDirtyBit and assign it to each register
        /// (all registers in the group share the same instance) which is useful for determining
        /// which groups of registers are dirty and need Apply() called.
        /// 
        /// RegManager assumes that the register array may be sparse (i.e. some of the
        /// array elements may be null).  However, other parts of the system may assume
        /// non-sparse arrays -- be sure to check usage of a group if you are adding a
        /// sparse array.
        /// 
        /// NOTE: if a register is added to the group *after* calling AddGroup() the dirty bit
        ///       needs to be "manually" added to the register.  For example:
        /// 
        ///            IRegister[] group = new IRegister[4];
        ///            mRegManager.AddGroup( "MyGroup", group ); // group[*] is null!!!
        ///            group[0] = new Reg32(...);  // register is in group but no dirty bit!
        ///            group[0].GroupDirtyBit = mRegManager.GetGroupDirtyBit( "MyGroup" );
        /// 
        /// NOTE: if a register belongs to multiple groups, it will only set the dirty bit for one of them!
        /// 
        /// NOTE: to retrieve the IRegisterSet use <see cref="GetRegisterSets"/>  or <see cref="GetRegisterSet"/>
        /// </summary>
        /// <param name="groupName">Name of group, must be non-null, unique, and not "*" (may be string.Empty)</param>
        /// <param name="group">collection of registers</param>
        /// <exception cref="InvalidParameterException">if the group already exists</exception>
        public void AddGroup( string groupName, IRegisterSet group )
        {
            lock( mGroupLock )
            {
                if( mGroups.ContainsKey( groupName ) ||
                    mRegisterSets.ContainsKey( groupName ) ||
                    mSuperGroups.ContainsKey( groupName ) )
                {
                    throw new InvalidParameterException(
                        string.Format( "RegManager '{0}' already contains group '{1}' (IRegisterSet)",
                                       Name,
                                       groupName ) );
                }
                // This adds the registers, creates the dirty bit
                AddGroup( groupName, group.Registers );
                // Add the register set...
                mRegisterSets[ groupName ] = group;
                // If we've already created the name lookup cache, refresh it
                if( mNameLookupReg.Count > 0 )
                {
                    UpdateRegisterDictionaries( true );
                }
            }
        }

        /// <summary>
        /// Define a "supergroup" (a group of groups). Normally, using a supergroup normally involves an extra
        /// level of indirection so for internal use please avoid using supergroups.  Supergroups are really
        /// intended to aid GUI interaction.
        /// 
        /// Supergroups do not have DirtyBits associated (because each register belongs to another group which
        /// has its own DirtyBit).
        /// </summary>
        /// <param name="groupName">Name of group, must be non-null, unique, and not "*" (may be string.Empty)</param>
        /// <param name="groups">Enumerable set of group names.</param>
        /// <remarks>
        /// SFP users may desire different grouping of registers than developers create. For example,
        /// developers will likely use RegisterSets to group registers by function but the user may
        /// wish to see all registers on a particular board.
        /// </remarks>
        public void AddGroup( string groupName, IEnumerable <string> groups )
        {
            lock( mGroupLock )
            {
                if( mGroups.ContainsKey( groupName ) ||
                    mRegisterSets.ContainsKey( groupName ) ||
                    mSuperGroups.ContainsKey( groupName ) )
                {
                    throw new InvalidParameterException(
                        string.Format( "RegManager '{0}' already contains group '{1}' (SuperGroup)",
                                       Name,
                                       groupName ) );
                }
                mSuperGroups[ groupName ] = groups;
                // If we've already created the name lookup cache, refresh it
                if( mNameLookupReg.Count > 0 )
                {
                    UpdateRegisterDictionaries( true );
                }
            }
        }

        /// <summary>
        /// Remove the named collection of registers.  If the group does not exist, this
        /// method exits without error.
        /// </summary>
        /// <param name="groupName">name of group to remove, case insensitive</param>
        public virtual void RemoveGroup( string groupName )
        {
            lock( mGroupLock )
            {
                if( mRegisterSets.ContainsKey( groupName ) )
                {
                    mRegisterSets.Remove( groupName );
                }
                if( mSuperGroups.ContainsKey( groupName ) )
                {
                    mSuperGroups.Remove( groupName );
                }
                if( mGroups.ContainsKey( groupName ) )
                {
                    mGroups.Remove( groupName );
                }
                if( mDirtyBits.ContainsKey( groupName ) )
                {
                    mDirtyBits.Remove( groupName );
                }
                // Force an update of the lookups
                UpdateRegisterDictionaries( true );
            }
        }

        /// <summary>
        /// Return the collection of registers under 'groupName' (that were added by AddGroup).
        /// 
        /// The collection is specifically an array instead of IEnumerable to insure that the
        /// collection can be indexed by int (or enum).
        /// 
        /// IMPORTANT: this returns a reference to the collection, NOT a copy.  DO NOT MODIFY.
        /// 
        /// RegManager assumes that the register array may be sparse (i.e. some of the
        /// array elements may be null).  However, other parts of the system may assume
        /// non-sparse arrays -- be sure to check usage of a group if you are adding a
        /// sparse array.
        /// </summary>
        /// <param name="groupName">name of group to retrieve, case insensitive</param>
        /// <returns></returns>
        /// <exception cref="InvalidParameterException">if the group does not exist</exception>
        public virtual IRegister[] GetGroup( string groupName )
        {
            lock( mGroupLock )
            {
                if( mGroups.ContainsKey( groupName ) )
                {
                    return mGroups[ groupName ];
                }
                if( mSuperGroups.ContainsKey( groupName ) )
                {
                    // For a "supergroup", aggregate the registers
                    List <IRegister> registers = new List <IRegister>();
                    foreach( string name in mSuperGroups[ groupName ] )
                    {
                        if( mGroups.ContainsKey( name ) )
                        {
                            registers.AddRange( mGroups[ name ] );
                        }
                    }
                    return registers.ToArray();
                }
                throw new InvalidParameterException( string.Format( "RegManager '{0}' does not contain group '{1}'",
                                                                    Name,
                                                                    groupName ) );
            }
        }

        /// <summary>
        /// Return a list of all the defined groups, excluding SuperGroups.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// SuperGroups are excluded because most usages of ListGroups are intended for iterating
        /// over all registers and including the SuperGroups would result in including some
        /// registers multiple times.
        /// </remarks>
        public virtual IEnumerable <string> ListGroups()
        {
            lock( mGroupLock )
            {
                return new List <string>( mGroups.Keys );
            }
        }

        /// <summary>
        /// Return a list of the groups included by 'GroupName'.  This is normally used to
        /// list the groups included by a SuperGroup.  A wildcard ('*') will result in a
        /// list of all groups (same result as ListGroups()).
        /// </summary>
        /// <param name="groupName">name of group to retrieve, case insensitive</param>
        /// <returns></returns>
        /// <remarks>
        /// SuperGroups are treated differently than Groups (i.e. not included in ListGroups)
        /// to avoid potential duplicate actions.
        /// </remarks>
        public IEnumerable <string> ListGroups( string groupName )
        {
            // Check for wildcard...
            if( groupName.Equals( "*" ) )
            {
                return ListGroups();
            }
            // Is it a supergroup?
            if( mSuperGroups.ContainsKey( groupName ) )
            {
                return new List <string>( mSuperGroups[ groupName ] );
            }
            // No, a non-supergroup is just itself
            return new List <string> { groupName };
        }

        /// <summary>
        /// Return a list of all the defined SuperGroups.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// SuperGroups are treated differently than Groups (i.e. not included in ListGroups)
        /// to avoid potential duplicate actions.
        /// </remarks>
        public IEnumerable <string> ListSuperGroups()
        {
            lock( mGroupLock )
            {
                return new List <string>( mSuperGroups.Keys );
            }
        }

        /// <summary>
        /// Return an enumerable list of the IRegister[] for each group -- this is intended for
        /// fast iteration over all groups/registers.  Typically this returns a reference to
        /// an internal structure -- SO DO NOT MODIFY THE RETURNED LIST.
        /// 
        /// RegManager assumes that the register array may be sparse (i.e. some of the
        /// array elements may be null).  However, other parts of the system may assume
        /// non-sparse arrays -- be sure to check usage of a group if you are adding a
        /// sparse array.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable <IRegister[]> GetGroups()
        {
            lock( mGroupLock )
            {
                return mGroups.Values;
            }
        }

        /// <summary>
        /// Return a list of all groups that were added as IRegisterSets (this encapsulates
        /// both the group's IDirtyBit as well as IRegister[] ... useful for dirty detection
        /// and register updating).
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable <IRegisterSet> GetRegisterSets()
        {
            lock( mGroupLock )
            {
                return mRegisterSets.Values;
            }
        }

        /// <summary>
        /// Return the IRegisterSet under 'GroupName' (that were added by AddGroup).
        /// 
        /// IMPORTANT: this returns a reference to the IRegisterSet instance, NOT a copy.  DO NOT MODIFY.
        /// </summary>
        /// <param name="groupName">name of group to retrieve, case insensitive</param>
        /// <returns></returns>
        /// <exception cref="InvalidParameterException">if the group does not exist</exception>
        public IRegisterSet GetRegisterSet( string groupName )
        {
            lock( mGroupLock )
            {
                if( mRegisterSets.ContainsKey( groupName ) )
                {
                    return mRegisterSets[ groupName ];
                }
                throw new InvalidParameterException(
                    string.Format( "RegManager '{0}' does not contain group '{1}' (IRegisterSet)",
                                   Name,
                                   groupName ) );
            }
        }

        /// <summary>
        /// Return the shared IDirtyBit for the registers under 'GroupName' (that were added by AddGroup).
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidParameterException">if the group does not exist</exception>
        public IDirtyBit GetGroupDirtyBit( string groupName )
        {
            lock( mGroupLock )
            {
                if( mDirtyBits.ContainsKey( groupName ) )
                {
                    return mDirtyBits[ groupName ];
                }
                throw new InvalidParameterException( string.Format( "RegManager '{0}' does not contain group '{1}'",
                                                                    Name,
                                                                    groupName ) );
            }
        }

        /// <summary>
        /// Indicate if any of the registers under 'GroupName' (that were added by AddGroup) are dirty
        /// (i.e. calling Apply() will write to hardware).
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidParameterException">if the group does not exist</exception>
        public bool IsGroupDirty( string groupName )
        {
            return GetGroupDirtyBit( groupName ).IsDirty;
        }

        /// <summary>
        /// Find and return the register identified by groupName and registerName. Depending on
        /// implementation, this may involve an iterative search and so may not be as fast as
        /// GetRegister( string, enum ) or GetRegister( string, int ).
        /// 
        /// Groups may contain the same register name, so this method will not always identify
        /// a unique register if '*' is used for the GroupName.
        /// </summary>
        /// <param name="groupName">name of group to search, case insensitive. Use "*" to search all groups</param>
        /// <param name="registerName">name of register to find, case insensitive</param>
        /// <returns>register, null if doesn't exist</returns>
        /// <exception cref="InvalidParameterException">if the group does not exist</exception>
        public virtual IRegister GetRegister( string groupName, string registerName )
        {
            lock( mGroupLock )
            {
                IRegister register = null;
                if( groupName == WILDCARD )
                {
                    foreach( var name in mGroups.Keys )
                    {
                        register = Find( name, registerName );
                        if( register != null )
                        {
                            break;
                        }
                    }
                }
                else if( mSuperGroups.ContainsKey( groupName ) )
                {
                    foreach( var name in mSuperGroups[ groupName ] )
                    {
                        register = Find( name, registerName );
                        if( register != null )
                        {
                            break;
                        }
                    }
                }
                else
                {
                    register = Find( groupName, registerName );
                }
                return register;
            }
        }

        /// <summary>
        /// Find and return the register identified by groupName and registerId.  Most implementations
        /// simply cast registerId to an int and index into the register collection, i.e.
        ///      return GetGroup( groupName )[(int)registerId]
        /// 
        /// Most projects will derive from RegManager and provide project-specific register access to
        /// avoid the lookup/casting overhead associated with this method.  E.g.
        /// 
        ///      class MyRegManager : RegManager {
        ///        MyRegManager() {
        ///           ...
        ///           mControlReg = mRegManager.GetReg( COMMON_GROUP, "Control" ) as Reg32;
        ///        }
        ///        ...
        ///        Reg32 ControlReg { get { return mControlReg; } }
        ///        ...
        ///      }
        ///      ...
        ///      Reg32 controlReg = mRegManager.ControlReg;
        /// </summary>
        /// <param name="groupName">name of group to search, case insensitive.</param>
        /// <param name="registerId">register identifier</param>
        /// <returns>register, null if doesn't exist</returns>
        public virtual IRegister GetRegister( string groupName, object registerId )
        {
            lock( mGroupLock )
            {
                if( mGroups.ContainsKey( groupName ) )
                {
                    return mGroups[ groupName ][ (int)registerId ];
                }
                throw new InvalidParameterException( string.Format( "RegManager '{0}' does not contain group '{1}'",
                                                                    Name,
                                                                    groupName ) );
            }
        }

        /// <summary>
        /// List the registers in the collection identified by groupName.  Each string identifies
        /// the register (by name) and TBD.  E.g.
        /// 
        ///     Control{,TBD,...}
        ///     IntMask{,TBD,...}
        /// 
        /// NOTE: for groupName=="*" (list all groups) there is the possibility of name collisions
        /// </summary>
        /// <param name="groupName">name of group to list, case insensitive. Use "*" to list all groups.</param>
        /// <returns></returns>
        public virtual IEnumerable <string> ListRegisters( string groupName )
        {
            lock( mGroupLock )
            {
                List <string> buffer = new List <string>();
                if( groupName == WILDCARD )
                {
                    foreach( var name in mGroups.Keys )
                    {
                        Append( name, ref buffer );
                    }
                }
                else
                {
                    Append( groupName, ref buffer );
                }
                return buffer;
            }
        }

        /// <summary>
        /// Update the dictionaries used to map name or address to a specific register object. This
        /// should be called if any registers are added/removed after module construction was finished
        /// and before calling FindRegister.
        /// </summary>
        /// <param name="force"></param>
        public void UpdateRegisterDictionaries( bool force )
        {
            lock( mLookupRegLock )
            {
                if( force || mNameLookupReg.Count == 0 )
                {
                    IEnumerable <string> groupNames = ListGroups();
                    foreach( var group in groupNames )
                    {
                        IRegister[] registers = GetGroup( group );
                        foreach( var register in registers )
                        {
                            // Ignore nulls...
                            if( register != null )
                            {
                                mAddressLookupReg[ register.Offset ] = register;
                                mNameLookupReg[ register.NameBase ] = register;
                                // Also create a group-specific name
                                string fullname = string.Format( "{0}[{1}]", group, register.NameBase );
                                mNameLookupReg[ fullname ] = register;
                            }
                        }
                    }
                    // Also build the index for SuperGroups
                    foreach( var group in ListSuperGroups() )
                    {
                        IRegister[] registers = GetGroup( group );
                        foreach( var register in registers )
                        {
                            // Ignore nulls...
                            if( register != null )
                            {
                                mAddressLookupReg[ register.Offset ] = register;
                                mNameLookupReg[ register.NameBase ] = register;
                                // Also create a group-specific name
                                string fullname = string.Format( "{0}[{1}]", group, register.NameBase );
                                mNameLookupReg[ fullname ] = register;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Find register with the specified address (offset from BAR).  For non-memory mapped registers, the offset
        /// is typically the index of a device register and there are typically "collisions" (i.e. searching for
        /// device registers by address will quite often fail)
        /// 
        /// This method lazily initializes a dictionary to lookup by address -- if multiple registers have the same
        /// address this method may not find the register of interest.  Use FindRegister( string, string ) to insure
        /// you find the correct register.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public IRegister FindRegister( int address )
        {
            lock( mLookupRegLock )
            {
                UpdateRegisterDictionaries( false );

                return ( mAddressLookupReg.ContainsKey( address ) ) ? mAddressLookupReg[ address ] : null;
            }
        }

        /// <summary>
        /// Find the register with the specified base name (e.g. GND15V) in the specified group.
        /// 
        /// This method lazily initializes a dictionary to lookup by BaseName or Group[BaseName] -- if multiple registers
        /// have the same BaseName this method may not find the register of interest if GroupName = "*" (wildcard).  Use
        /// the specific group name to insure you find the correct register
        /// </summary>
        /// <param name="groupName">use "*" to search all groups</param>
        /// <param name="name"></param>
        /// <returns></returns>
        public IRegister FindRegister( string groupName, string name )
        {
            lock( mLookupRegLock )
            {
                UpdateRegisterDictionaries( false );

                // name may have an embedded group
                if( name.Contains( "." ) )
                {
                    char[] separator = { '.' };
                    string[] tokens = name.Split( separator, StringSplitOptions.RemoveEmptyEntries );
                    if( tokens.Length == 2 )
                    {
                        return FindRegister( tokens[ 0 ], tokens[ 1 ] );
                    }
                }

                // The key depends on groupName
                string key = ( groupName == "*" ) ? name : string.Format( "{0}[{1}]", groupName, name );

                return ( mNameLookupReg.ContainsKey( key ) ) ? mNameLookupReg[ key ] : null;
            }
        }

        /// <summary>
        /// Set every known register to "dirty" (IRegister.NeedApply=true). This is effectively
        /// the same as setting "force=true" for the next IRegister.Apply(force).  
        /// </summary>
        /// <remarks>
        /// There are very few cases where it is appropriate to use this method because
        /// it results in a rewrite of every register on the next Apply(). USE WITH CAUTION
        /// </remarks>
        public void ForceDirty()
        {
            lock( mGroupLock )
            {
                foreach( var group in GetGroups() )
                {
                    if( group != null )
                    {
                        ForceDirty( group );
                    }
                }
                
            }
        }

        /// <summary>
        /// Set every register in the specified group to "dirty" (IRegister.NeedApply=true). This
        /// is effectively the same as setting "force=true" for the next IRegister.Apply(force).  
        /// </summary>
        /// <remarks>
        /// There are very few cases where it is appropriate to use this method because
        /// it results in a rewrite of every register on the next Apply(). USE WITH CAUTION
        /// </remarks>
        public void ForceDirty( IRegister[] group )
        {
            foreach( var register in group )
            {
                if( register != null )
                {
                    register.NeedApply = true;
                }
            }
        }

        #endregion

        #region protected implementation/helpers

        /// <summary>
        /// Append the "register list" (format TBD) for the specified group
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="list"></param>
        protected virtual void Append( string groupName, ref List <string> list )
        {
            if( mGroups.ContainsKey( groupName ) )
            {
                IRegister[] group = mGroups[ groupName ];
                foreach( var register in group )
                {
                    if( register != null )
                    {
                        // TODO CORE: include other info (reg type, size, etc.)?
                        list.Add( register.Name );
                    }
                }
            }
            else if( mSuperGroups.ContainsKey( groupName ) )
            {
                // For supergroups, recursively add each group
                foreach( var name in mSuperGroups[ groupName ] )
                {
                    Append( name, ref list );
                }
            }
            else
            {
                throw new InvalidParameterException( string.Format( "RegManager '{0}' does not contain group '{1}'",
                                                                    Name,
                                                                    groupName ) );
            }
        }

        /// <summary>
        /// Find the named register in the named group...
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="registerName"></param>
        /// <returns></returns>
        protected virtual IRegister Find( string groupName, string registerName )
        {
            // TODO CORE: build/cache dictionary/map of register names and use that!
            if( mGroups.ContainsKey( groupName ) )
            {
                IRegister[] group = mGroups[ groupName ];
                foreach( var register in group )
                {
                    if( register != null &&
                        ( register.Name.Equals( registerName, StringComparison.CurrentCultureIgnoreCase ) ||
                          register.NameBase.Equals( registerName, StringComparison.CurrentCultureIgnoreCase ) ) )
                    {
                        return register;
                    }
                }
                return null;
            }
            throw new InvalidParameterException( string.Format( "RegManager '{0}' does not contain group '{1}'",
                                                                Name,
                                                                groupName ) );
        }

        #endregion protected implementation/helpers
    }
}
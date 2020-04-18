/******************************************************************************
 *                                                                         
 *               
 *               All rights reserved.
 *
 *****************************************************************************/

using System.Collections.Generic;
using InstrumentDriver.Core.Utility;

namespace InstrumentDriver.Core.Register
{
    /// <summary>
    /// IRegManager defines a generic register management interface.  
    /// 
    /// The register collections are specifically an array instead of IEnumerable to insure that the
    /// collection can be quickly indexed by int (or enum).
    /// 
    /// Certainly it is conceivable that a project may want multi-dimensional register collections
    /// (e.g. Registers[board][interface][instrument]), but rather than defining many signatures for
    /// AddGroup, RemoveGroup, GetRegister, etc. the group index is a string which could be encoded
    /// for many dimensions (e.g. Registers["Board1:Interface3:Instrument5"])
    /// 
    /// In theory, any module could use an implementation of this interface to access its registers, e.g.
    /// 
    ///      Reg32 controlReg = mRegManager.GetReg( COMMON_GROUP, "Control" ) as Reg32;
    /// 
    /// But normally a project will extend the interface to provide caching (for performance) and
    /// appropriate typing (for code simplicity), e.g.
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
    public interface IRegManager
    {
        /// <summary>
        /// Name of register manager (useful for identifying specific instance if a module supports
        /// multiple instances of IRegManager).
        /// </summary>
        string Name
        {
            get;
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
        /// <param name="GroupName">Name of group, must be non-null, unique, and not "*" (may be string.Empty)</param>
        /// <param name="Group">collection of registers</param>
        /// <exception cref="InvalidParameterException">if the group already exists</exception>
        void AddGroup( string GroupName, IRegister[] Group );

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
        /// <param name="GroupName">Name of group, must be non-null, unique, and not "*" (may be string.Empty)</param>
        /// <param name="Group">collection of registers</param>
        /// <exception cref="InvalidParameterException">if the group already exists</exception>
        void AddGroup( string GroupName, IRegisterSet Group );

        /// <summary>
        /// Define a "supergroup" (a group of groups). Normally, using a supergroup normally involves an extra
        /// level of indirection so for internal use please avoid using supergroups.  Supergroups are really
        /// intended to aid GUI interaction.
        /// </summary>
        /// <param name="GroupName">Name of group, must be non-null, unique, and not "*" (may be string.Empty)</param>
        /// <param name="Groups">Enumerable set of group names.</param>
        /// <remarks>
        /// SFP users may desire different grouping of registers than developers create. For example,
        /// developers will likely use RegisterSets to group registers by function but the user may
        /// wish to see all registers on a particular board.
        /// </remarks>
        void AddGroup( string GroupName, IEnumerable <string> Groups );

        /// <summary>
        /// Remove the named collection of registers.  If the group does not exist, this
        /// method exits without error.
        /// </summary>
        /// <param name="GroupName">name of group to remove, case insensitive</param>
        void RemoveGroup( string GroupName );

        /// <summary>
        /// Return the collection of registers under 'GroupName' (that were added by AddGroup).
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
        /// <param name="GroupName">name of group to retrieve, case insensitive</param>
        /// <returns></returns>
        /// <exception cref="InvalidParameterException">if the group does not exist</exception>
        IRegister[] GetGroup( string GroupName );

        /// <summary>
        /// Return a list of all the defined groups, excluding SuperGroups.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// SuperGroups are excluded because most usages of ListGroups are intended for iterating
        /// over all registers and including the SuperGroups would result in including some
        /// registers multiple times.
        /// </remarks>
        IEnumerable <string> ListGroups();

        /// <summary>
        /// Return a list of the groups included by 'GroupName'.  This is normally used to
        /// list the groups included by a SuperGroup.  A wildcard ('*') will result in a
        /// list of all groups (same result as ListGroups()).
        /// </summary>
        /// <param name="GroupName">name of group to retrieve, case insensitive</param>
        /// <returns></returns>
        /// <remarks>
        /// SuperGroups are treated differently than Groups (i.e. not included in ListGroups)
        /// to avoid potential duplicate actions.
        /// </remarks>
        IEnumerable <string> ListGroups( string GroupName );

        /// <summary>
        /// Return a list of all the defined SuperGroups.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// SuperGroups are treated differently than Groups (i.e. not included in ListGroups)
        /// to avoid potential duplicate actions.
        /// </remarks>
        IEnumerable <string> ListSuperGroups();

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
        IEnumerable <IRegister[]> GetGroups();

        /// <summary>
        /// Return a list of all groups that were added as IRegisterSets (this encapsulates
        /// both the group's IDirtyBit as well as IRegister[] ... useful for dirty detection
        /// and register updating).
        /// </summary>
        /// <returns></returns>
        IEnumerable <IRegisterSet> GetRegisterSets();

        /// <summary>
        /// Return the IRegisterSet under 'GroupName' (that were added by AddGroup).
        /// 
        /// IMPORTANT: this returns a reference to the IRegisterSet instance, NOT a copy.  DO NOT MODIFY.
        /// </summary>
        /// <param name="GroupName">name of group to retrieve, case insensitive</param>
        /// <returns></returns>
        /// <exception cref="InvalidParameterException">if the group does not exist</exception>
        IRegisterSet GetRegisterSet( string GroupName );

        /// <summary>
        /// Return the shared IDirtyBit for the registers under 'GroupName' (that were added by AddGroup).
        /// </summary>
        /// <param name="GroupName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidParameterException">if the group does not exist</exception>
        IDirtyBit GetGroupDirtyBit( string GroupName );

        /// <summary>
        /// Indicate if any of the registers under 'GroupName' (that were added by AddGroup) are dirty
        /// (i.e. calling Apply() will write to hardware).
        /// </summary>
        /// <param name="GroupName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidParameterException">if the group does not exist</exception>
        bool IsGroupDirty( string GroupName );

        /// <summary>
        /// Find and return the register identified by GroupName and RegisterName. Depending on
        /// implementation, this may involve an iterative search and so may not be as fast as
        /// GetRegister( string, enum ) or GetRegister( string, int ).
        /// 
        /// Groups may contain the same register name, so this method will not always identify
        /// a unique register if '*' is used for the GroupName.
        /// </summary>
        /// <param name="GroupName">name of group to search, case insensitive. Use "*" to search all groups</param>
        /// <param name="RegisterName">name of register to find, case insensitive</param>
        /// <returns>register, null if doesn't exist</returns>
        /// <exception cref="InvalidParameterException">if the group does not exist</exception>
        IRegister GetRegister( string GroupName, string RegisterName );

        /// <summary>
        /// Find and return the register identified by GroupName and RegisterId.  Most implementations
        /// simply cast RegisterId to an int and index into the register collection, i.e.
        ///      return GetGroup( GroupName )[(int)RegisterId]
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
        /// <param name="GroupName">name of group to search, case insensitive.</param>
        /// <param name="RegisterId">register identifier</param>
        /// <returns>register, null if doesn't exist</returns>
        IRegister GetRegister( string GroupName, object RegisterId );

        /// <summary>
        /// List the registers in the collection identified by GroupName.  Each string identifies
        /// the register (by name) and TBD.  E.g.
        /// 
        ///     Control{,TBD,...}
        ///     IntMask{,TBD,...}
        /// 
        /// NOTE: for GroupName=="*" (list all groups) there is the possibility of name collisions
        /// </summary>
        /// <param name="GroupName">name of group to list, case insensitive. Use "*" to list all groups.</param>
        /// <returns></returns>
        IEnumerable <string> ListRegisters( string GroupName );

        /// <summary>
        /// Update the dictionaries used to map name or address to a specific register object. This
        /// should be called if any registers are added/removed after module construction was finished
        /// and before calling FindRegister.
        /// </summary>
        /// <param name="force"></param>
        void UpdateRegisterDictionaries( bool force );

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
        IRegister FindRegister( int address );

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
        IRegister FindRegister( string groupName, string name );

        /// <summary>
        /// Set every known register to "dirty" (IRegister.NeedApply=true). This is effectively
        /// the same as setting "force=true" for the next IRegister.Apply(force).  
        /// </summary>
        /// <remarks>
        /// There are very few cases where it is appropriate to use this method because
        /// it results in a rewrite of every register on the next Apply(). USE WITH CAUTION
        /// </remarks>
        void ForceDirty();

        /// <summary>
        /// Set every register in the specified group to "dirty" (IRegister.NeedApply=true). This
        /// is effectively the same as setting "force=true" for the next IRegister.Apply(force).  
        /// </summary>
        /// <remarks>
        /// There are very few cases where it is appropriate to use this method because
        /// it results in a rewrite of every register on the next Apply(). USE WITH CAUTION
        /// </remarks>
        void ForceDirty( IRegister[] group );
    }
}
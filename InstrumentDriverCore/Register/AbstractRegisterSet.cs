using System;
using System.Collections.Generic;
using System.Text;
using InstrumentDriver.Core.Utility;
using InstrumentDriver.Core.Interfaces;

namespace InstrumentDriver.Core.Register
{
    /// <summary>
    /// AbstractRegisterSet contains the implementation of IRegisterSet.  The derived
    /// class must define the registers, generally in its constructor(s).
    /// </summary>
    public abstract class AbstractRegisterSet : IRegisterSet
    {
        #region Implementation of IRegisterSet

        /// <summary>
        /// Return a vector of the IRegisters that make up the RegisterSet.  Typically
        /// this is created during construction of the class that implements this
        /// interface.
        /// </summary>
        public IRegister[] Registers
        {
            get;
            protected set;
        }

        /// <summary>
        /// Get the DirtyBit for the RegisterSet.  Since all Grouped registers share
        /// the same GroupDirtyBit, in most circumstances simply returning the GroupDirtyBit
        /// of the first entry in the Registers is sufficient.
        /// </summary>
        public IDirtyBit GroupDirtyBit
        {
            get
            {
                return Registers[ 0 ].GroupDirtyBit;
            }
        }

        /// <summary>
        /// Set the initial values of registers.  Each implementation decide what, if any,
        /// register values need to be set and/or written.  The default implementation
        /// is a NOP.
        /// </summary>
        public virtual void SetInitialValues()
        {
            // NOP!
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Initialize the register accessors. These are normally public properties to
        /// expose commonly accessed registers and/or wrap registers in Reg32T.
        /// </summary>
        /// <remarks>
        /// The default implementation in AbstractRegisterSet is a NOP.
        /// </remarks>
        protected virtual void InitializeRegisterAccessors()
        {
            // NOP
        }

        /// <summary>
        /// Construct the specified registers and bitfields.
        /// 
        /// Also, set up the register accessors (by calling virtual method InitializeRegisterAccessors)
        /// </summary>
        /// <param name="manager">Optional IRegManager, if non-null the created registers will be added as a group</param>
        /// <param name="module">The module the register will be part of</param>
        /// <param name="groupName">The group name to use when adding register to 'manager'</param>
        /// <param name="factory">The register factory to use to create the registers</param>
        /// <param name="registerEnumType">The type of the register enumeration, used to size the register array and determine register names</param>
        /// <param name="registers">The register definitions</param>
        /// <param name="bitFields">The bit field definitions</param>
        protected void ConstructRegisters( IRegManager manager,
                                           IInstrument module,
                                           string groupName,
                                           RegFactory factory,
                                           Type registerEnumType,
                                           ICollection <RegDef> registers,
                                           ICollection <BitFieldDef> bitFields )
        {
            const object[] args = null;
            ConstructRegisters( manager, module, groupName, factory, registerEnumType, registers, bitFields, args );
        }

        /// <summary>
        /// Construct the specified registers and bitfields.
        /// 
        /// Also, set up the register accessors (by calling virtual method InitializeRegisterAccessors)
        /// </summary>
        /// <param name="manager">Optional IRegManager, if non-null the created registers will be added as a group</param>
        /// <param name="module">The module the register will be part of</param>
        /// <param name="groupName">The group name to use when adding register to 'manager'</param>
        /// <param name="factory">The register factory to use to create the registers</param>
        /// <param name="registerEnumType">The type of the register enumeration, used to size the register array and determine register names</param>
        /// <param name="registers">The register definitions</param>
        /// <param name="bitFields">The bit field definitions</param>
        /// <param name="args">optional arguments to pass to CreateRegArray - interpretation depends on the specific RegFactory implementation</param>
        protected void ConstructRegisters( IRegManager manager,
                                           IInstrument module,
                                           string groupName,
                                           RegFactory factory,
                                           Type registerEnumType,
                                           ICollection <RegDef> registers,
                                           ICollection <BitFieldDef> bitFields,
                                           object[] args )
        {
            string middleName = string.Empty;
            Registers = factory.CreateRegArray( registers,
                                                registerEnumType,
                                                module.RegDriver,
                                                module.Name,
                                                middleName,
                                                args );

            factory.CreateBitFields( bitFields,
                                     Registers,
                                     module.Name );

            // TODO CORE: should this be optional?
            // Verify bit fields...
            foreach( IRegister register in Registers )
            {
                if( register != null )
                {
                    register.VerifyBitFields();
                }
            }

            // "Install" the registers into IRegManager
            if( manager != null && ! string.IsNullOrEmpty( groupName ) )
            {
                manager.AddGroup( groupName, Registers );
            }

            // Set up register accessors (normally public properties to expose commonly accessed
            // registers and/or wrap registers in Reg32T).
            InitializeRegisterAccessors();
        }

        /// <summary>
        /// Replace the BitField type defined in supplied the register and bit field definitions.
        /// This allows a derived class to customize a RegisterSet (e.g. use SlugCarrierEventBF
        /// instead of EventBF).
        /// </summary>
        /// <param name="registerDefinitions">The register definitions to filter</param>
        /// <param name="bitFieldDefinitions">The bit field definitions to filter</param>
        /// <param name="oldType">the old/original Type of the bit field</param>
        /// <param name="newType">the new Type of the bit field</param>
        protected static void ReplaceType( IList <RegDef> registerDefinitions,
                                           IList <BitFieldDef> bitFieldDefinitions,
                                           Type oldType,
                                           Type newType )
        {
            // Replace the bit field type for any register definitions using 'oldType'
            for( int j = registerDefinitions.Count - 1; j >= 0; j -- )
            {
                RegDef definition = registerDefinitions[ j ];
                if( ReferenceEquals( oldType, definition.BFenum ) )
                {
                    definition.BFenum = newType;
                }
            }
            // Replace the bit field type for and bit field definitions using 'oldType'
            for( int j = bitFieldDefinitions.Count - 1; j >= 0; j-- )
            {
                BitFieldDef definition = bitFieldDefinitions[ j ];
                if( ReferenceEquals( oldType, definition.BFType ) )
                {
                    definition.BFType = newType;
                }
            }
        }

        /// <summary>
        /// Remove the register definition identified by 'id' (e.g. (int)eCommonReg.Trig10) from
        /// 'registerDefinitions'.
        /// </summary>
        /// <param name="registerDefinitions">The register definitions to filter</param>
        /// <param name="id">The register's enum value (e.g. (int)eCommonReg.Trig10) of the definition to remove</param>
        public static void RemoveRegister( IList <RegDef> registerDefinitions, int id )
        {
            for( int j = registerDefinitions.Count - 1; j >= 0; j-- )
            {
                RegDef register = registerDefinitions[ j ];
                if( register.nameEnum == id )
                {
                    registerDefinitions.RemoveAt( j );
                    // There should only be 1 entry per register ... so we're done
                    break;
                }
            }
        }

        /// <summary>
        /// Remove the register definition(s) identified by 'id' (e.g. (int)eCommonReg.Trig10) from
        /// 'registerDefinitions'.
        /// </summary>
        /// <param name="registerDefinitions">The register definitions to filter</param>
        /// <param name="id">List of the registers's enum value (e.g. (int)eCommonReg.Trig10) of the definition to remove</param>
        public static void RemoveRegisters( IList <RegDef> registerDefinitions, ICollection <int> id )
        {
            // Index backwards so deletions don't affect where we need to check next
            for( int j = registerDefinitions.Count - 1; j >= 0; j-- )
            {
                RegDef register = registerDefinitions[ j ];
                if( id.Contains( register.nameEnum ) )
                {
                    registerDefinitions.RemoveAt( j );
                }
            }
        }

        /// <summary>
        /// Remove any entries in 'bitFieldDefinitions' identified by 'bitFieldType' and 'index'.
        /// This must be called before <see cref="ConstructRegisters"/>
        /// </summary>
        /// <param name="registerDefinitions">The registers definitions, used to determine bit field type if bitFieldDefinition[j] specifies a register instead of a type</param>
        /// <param name="bitFieldDefinitions">The bit field definitions to filter</param>
        /// <param name="bitFieldType">The Type of the bit fields to remove, e.g. typeof( EventBF )</param>
        /// <param name="index">The specific bit field to remove, e.g. EventBF.ABusComplete</param>
        /// <remarks>
        /// This is normally used to remove bit fields defined by a common/shared definition (e.g.
        /// CommonRegisterSet) that should not be used for a specific module. For example:
        /// 
        ///     RemoveBitField( registerDefs, bitFieldDefs, typeof( SlugCarrierEventBF ), (int)SlugCarrierEventBF.CascadeEvent );
        /// 
        /// NOTE: any type replacement has been done by the default constructor, so bitFieldType
        ///       should refer to the module specific type 
        /// </remarks>
        public static void RemoveBitField( IList <RegDef> registerDefinitions,
                                           IList <BitFieldDef> bitFieldDefinitions,
                                           Type bitFieldType,
                                           int index )
        {
            // Cache of register to bit field Type
            Dictionary <int, Type> registerToBitFieldType = null;

            // Index backwards so deletions don't affect where we need to check next
            for( int j = bitFieldDefinitions.Count - 1; j >= 0; j-- )
            {
                BitFieldDef def = bitFieldDefinitions[ j ];
                Type defBitFieldType = def.BFType;
                if( defBitFieldType == null )
                {
                    // The definition does not include a bit field Type ... 
                    // Lazily construct the cache of register to bit field Type
                    if( registerToBitFieldType == null )
                    {
                        registerToBitFieldType = new Dictionary <int, Type>();
                        foreach( var register in registerDefinitions )
                        {
                            registerToBitFieldType[ register.nameEnum ] = register.BFenum;
                        }
                    }
                    // Now determine the bit field type from the register definition
                    if( registerToBitFieldType.ContainsKey( def.RegEnum ) )
                    {
                        defBitFieldType = registerToBitFieldType[ def.RegEnum ];
                    }
                }
                if( ReferenceEquals( bitFieldType, defBitFieldType ) && def.nameEnum == index )
                {
                    bitFieldDefinitions.RemoveAt( j );
                    // Continue searching ... there may be multiple definitions that match
                }
            }
        }

        /// <summary>
        /// Remove any entries in 'bitFieldDefinitions' identified by 'bitFieldType'.
        /// This must be called before <see cref="ConstructRegisters"/>
        /// </summary>
        /// <param name="registerDefinitions">The registers definitions, used to determine bit field type if bitFieldDefinition[j] specifies a register instead of a type</param>
        /// <param name="bitFieldDefinitions">The bit field definitions to filter</param>
        /// <param name="bitFieldType">The Type of the bit fields to remove, e.g. typeof( EventBF )</param>
        /// <remarks>
        /// This is normally used to remove bit fields defined by a common/shared definition (e.g.
        /// CommonRegisterSet) that should not be used for a specific module. For example:
        /// 
        ///     RemoveBitField( registerDefs, bitFieldDefs, typeof( SlugCarrierEventBF ), (int)SlugCarrierEventBF.CascadeEvent );
        /// 
        /// NOTE: any type replacement has been done by the default constructor, so bitFieldType
        ///       should refer to the module specific type 
        /// </remarks>
        public static void RemoveBitFields( IList <RegDef> registerDefinitions,
                                            IList <BitFieldDef> bitFieldDefinitions,
                                            Type bitFieldType )
        {
            // Cache of register to bit field Type
            Dictionary <int, Type> registerToBitFieldType = null;

            // Index backwards so deletions don't affect where we need to check next
            for( int j = bitFieldDefinitions.Count - 1; j >= 0; j-- )
            {
                BitFieldDef def = bitFieldDefinitions[ j ];
                Type defBitFieldType = def.BFType;
                if( defBitFieldType == null )
                {
                    // The definition does not include a bit field Type ... 
                    // Lazily construct the cache of register to bit field Type
                    if( registerToBitFieldType == null )
                    {
                        registerToBitFieldType = new Dictionary <int, Type>();
                        foreach( var register in registerDefinitions )
                        {
                            registerToBitFieldType[ register.nameEnum ] = register.BFenum;
                        }
                    }
                    // Now determine the bit field type from the register definition
                    if( registerToBitFieldType.ContainsKey( def.RegEnum ) )
                    {
                        defBitFieldType = registerToBitFieldType[ def.RegEnum ];
                    }
                }
                if( ReferenceEquals( bitFieldType, defBitFieldType ) )
                {
                    bitFieldDefinitions.RemoveAt( j );
                    // Continue searching ... there may be multiple definitions that match
                }
            }
        }

#if DEBUG
        /// <summary>
        /// Dump the register definitions for the specified registers.  More...
        /// </summary>
        /// <param name="registers"></param>
        /// <returns></returns>
        /// <remarks>
        /// The intent is to get a list of "what have I got" which could be used for before/after
        /// comparison or replacing handcoded register sets with table driven register sets.
        /// </remarks>
        public static string DumpRegisters( IRegister[] registers )
        {
            // TODO CORE:
            // NOTE: this could be as simple as 'foreach( ... ) { register.ToString(); }' as long
            // as ToString dumps sufficient details.
            return "TODO";
        }

        /// <summary>
        /// Utility function to compare two sets of registers with the intent of ensuring
        /// replacing the "old style" of register creation with RegisterSets results in
        /// the same register and bit field collections
        /// </summary>
        /// <param name="name">name of register collection</param>
        /// <param name="referenceSet">register collection to be matched</param>
        /// <param name="candidateSet">register collection that should match 'referenceSet'</param>
        /// <returns>Description of the differences</returns>
        public static string Compare( string name, IRegister[] referenceSet, IRegister[] candidateSet )
        {
            StringBuilder buffer = new StringBuilder();
            buffer.AppendFormat( "------ Compare( '{0}', IRegister[], IRegister[] ) ------\n", name );
            if( referenceSet == null && candidateSet == null )
            {
                buffer.AppendFormat( "Info: both register sets are null\n" );
                return buffer.ToString();
            }
            if( referenceSet == null )
            {
                buffer.AppendFormat( "Extra: referenceSet is null, candidateSet[{0}]\n", candidateSet.Length );
                return buffer.ToString();
            }
            if( candidateSet == null )
            {
                buffer.AppendFormat( "Missing: referenceSet[{0}], candidateSet is null\n", referenceSet.Length );
                return buffer.ToString();
            }
            for( int index = 0; index < referenceSet.Length; index++ )
            {
                try
                {
                    IRegister reference = referenceSet[ index ];
                    IRegister candidate = ( index < candidateSet.Length ) ? candidateSet[ index ] : null;
                    if( reference == null )
                    {
                        if( candidate != null )
                        {
                            buffer.AppendFormat( "Extra:   referenceSet does not contain {0}\n", candidate.Name );
                        }
                        continue;
                    }
                    if( candidate == null )
                    {
                        buffer.AppendFormat( "Missing: candidateSet does not contain {0}\n", reference.Name );
                        continue;
                    }
                    if( string.Compare( reference.Name, candidate.Name, StringComparison.InvariantCultureIgnoreCase ) !=
                        0 )
                    {
                        buffer.AppendFormat( "Naming:  {0} != {1}\n", reference.Name, candidate.Name );
                        continue;
                    }
                    if( reference.RegType != candidate.RegType )
                    {
                        buffer.AppendFormat( "RegType:  {0}.{1} != {2}.{3}\n",
                                             reference.NameBase,
                                             reference.RegType,
                                             candidate.NameBase,
                                             candidate.RegType );
                    }
                    if( reference.Offset != candidate.Offset )
                    {
                        buffer.AppendFormat( "Offset:  {0}.0x{1:x} != {2}.0x{3:x}\n",
                                             reference.NameBase,
                                             reference.Offset,
                                             candidate.NameBase,
                                             candidate.Offset );
                    }
                    if( reference.NumBitFields != candidate.NumBitFields ||
                        reference.FirstBF != candidate.FirstBF ||
                        reference.LastBF != candidate.LastBF )
                    {
                        buffer.AppendFormat( "Bitfield: {6} # fields ({0},{1},{2}) vs. ({3},{4},{5})\n",
                                             reference.NumBitFields,
                                             reference.FirstBF,
                                             reference.LastBF,
                                             candidate.NumBitFields,
                                             candidate.FirstBF,
                                             candidate.LastBF,
                                             reference.Name );
                        continue;
                    }
                    if( reference.NumBitFields > 0 )
                    {
                        for( int j = reference.FirstBF; j < reference.LastBF; j++ )
                        {
                            IBitField referenceBF = reference.GetField( (uint)j );
                            IBitField candidateBF = candidate.GetField( (uint)j );
                            if( referenceBF == null )
                            {
                                if( candidateBF != null )
                                {
                                    buffer.AppendFormat( "ExtraBF: reference does not contain {0}.{1}\n",
                                                         reference.Name,
                                                         candidateBF.Name );
                                }
                                continue;
                            }
                            if( candidateBF == null )
                            {
                                buffer.AppendFormat( "MissingBF: candidate does not contain {0}.{1}\n",
                                                     reference.Name,
                                                     referenceBF.Name );
                                continue;
                            }
                            if( string.Compare( referenceBF.Name,
                                                candidateBF.Name,
                                                StringComparison.InvariantCultureIgnoreCase ) != 0 )
                            {
                                buffer.AppendFormat( "NameBF: {0} != {1}\n", referenceBF.Name, candidateBF.Name );
                                continue;
                            }
                            if( referenceBF.StartBit != candidateBF.StartBit ||
                                referenceBF.EndBit != candidateBF.EndBit ||
                                referenceBF.Mask != candidateBF.Mask )
                            {
                                buffer.AppendFormat( "MismatchBF: {6}.{7} ({0},{1},0x{2:x}) vs ({3},{4},0x{5:x})\n",
                                                     referenceBF.StartBit,
                                                     referenceBF.EndBit,
                                                     referenceBF.Mask,
                                                     candidateBF.StartBit,
                                                     candidateBF.EndBit,
                                                     candidateBF.Mask,
                                                     reference.Name,
                                                     referenceBF.Name );
                            }
                        }
                    }
                }
                catch( Exception ex )
                {
                    buffer.AppendFormat( "Error at index {0}: {1}\n", index, ex.Message );
                }
            }
            for( int index = referenceSet.Length; index < candidateSet.Length; index++ )
            {
                IRegister candidate = candidateSet[ index ];
                if( candidate != null )
                {
                    buffer.AppendFormat( "Extra: referenceSet does not contain {0}\n", candidate.Name );
                }
            }
            return buffer.ToString();
        }
#endif

        #endregion Helpers
    }
}
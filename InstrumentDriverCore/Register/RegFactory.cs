/******************************************************************************
 *                                                                         
 *               
 *               All rights reserved.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using InstrumentDriver.Core.Interfaces;
using InstrumentDriver.Core.Utility;

namespace InstrumentDriver.Core.Register
{
    /// <summary>
    /// Delegate method used to construct a register.  Normally, every register implementation
    /// (e.g. Reg32, Reg64) will provide a static method (typically 'ConstructReg') that implements
    /// this delegate and is used in by RegFactory.CreateRegArray to manufacture registers.
    /// </summary>
    /// <param name="name">register name</param>
    /// <param name="regDef">reference to RegDef struct.</param>
    /// <param name="driver">driver to read this type of register.</param>
    /// <param name="baseAddr">typically 0, but this additional base address can be
    /// used to add to the register address specified in the RegDef struct. 
    /// Currently only used for CannotReadDirectly registers, so is typically 0.</param>
    /// <param name="args">arbitrary array of objects for use by the delegate ... normally
    /// used to specify register-type specific arguments</param>
    /// <returns>a reference to the register created.</returns>
    public delegate IRegister ConstructRegDelegate( string name,
                                                    RegDef regDef,
                                                    IRegDriver driver,
                                                    int baseAddr,
                                                    object[] args );

    /// <summary>
    /// Delegate method used to construct a field. Any unusual field implementations (other than
    /// RegField32 and RegField64) will provide a static method (typically 'ConstructField') that
    /// implements this delegate and is used by RegFactory.CreateBitFields to manufacture fields.
    /// </summary>
    /// <param name="bitFieldName">field name</param>
    /// <param name="nameEnum">integer id of field, normally (int)EnumType.Value</param>
    /// <param name="startBit">start bit, zero based index</param>
    /// <param name="sizeInBits">width of field in bits</param>
    /// <param name="reg">the register that the newly created field will be added to</param>
    /// <param name="args">opaque (to RegFactory) arguments passed from BitFieldDefBase.Args</param>
    /// <returns></returns>
    public delegate IBitField ConstructFieldDelegate( string bitFieldName,
                                                      int nameEnum,
                                                      int startBit,
                                                      int sizeInBits,
                                                      IRegister reg,
                                                      object args );

    /// <summary>
    /// The factory class that creates Registers and BitFields in the system.
    /// </summary>
    public class RegFactory
    {
        #region constructors and member variables 

        private readonly IInstrument mModule;
        private readonly Int32 mBaseAddr; // only used by CannotReadDirectly registers.
        /// <summary>
        /// The "default" register driver to use during register construction
        /// </summary>
        private readonly IRegDriver mRegDriver;
        /// <summary>
        /// The module's collection of register drivers indexed by BAR # (e.g. BAR0==0)
        /// </summary>
        private readonly IRegDriver[] mRegDrivers;
        private readonly String mModuleName;
        private static bool mIsRegistered;

        private static readonly Dictionary <string, Type> mRegisterTypes =
            new Dictionary <string, Type>( StringComparer.InvariantCultureIgnoreCase );

        public RegFactory( int baseAddr,
                           IInstrument module,
                           ConstructRegDelegate constructRegister,
                           ConstructFieldDelegate constructField )
        {
            // If the "well-known" register types haven't been added, do that now...
            lock( mRegisterTypes )
            {
                if( !mIsRegistered )
                {
                    Reg32.RegisterTypeWithFactory();
                    Reg64.RegisterTypeWithFactory();
                    AddrDataReg32.RegisterTypeWithFactory();
                    mIsRegistered = true;
                }
            }

            mModule = module;
            mBaseAddr = baseAddr;
            if( module == null )
            {
                mModuleName = "Unknown";
            }
            else
            {
                mModuleName = module.Name;
                mRegDriver = module.RegDriver;
                mRegDrivers = module.RegDrivers;
            }
            ConstructReg = constructRegister;
            ConstructField = constructField;
        }

        public RegFactory( IInstrument module, ConstructRegDelegate construct, ConstructFieldDelegate constructField ) :
            this( 0, module, construct, constructField )
        {
        }

        public RegFactory( int baseAddr, IInstrument module, ConstructRegDelegate construct ) :
            this( baseAddr, module, construct, null )
        {
        }

        public RegFactory( int baseAddr, IInstrument module ) :
            this( baseAddr, module, Reg32.ConstructReg, null )
        {
        }

        public RegFactory() :
            this( 0, null, Reg32.ConstructReg, null )
        {
        }

        public RegFactory( IInstrument mb, ConstructRegDelegate construct ) :
            this( 0, mb, construct, null )
        {
        }

        public RegFactory( IInstrument mb ) :
            this( 0, mb, Reg32.ConstructReg, null )
        {
        }

        #endregion constructors and member variables 

        #region factory support and implementation

        protected ConstructRegDelegate ConstructReg
        {
            get;
            set;
        }

        protected ConstructFieldDelegate ConstructField
        {
            get;
            set;
        }

        /// <summary>
        /// creates a register or BitField Name of "moduleName_middleName_ItemName"
        /// </summary>
        /// <param name="itemEnumType">The enum list the ItemName comes from.</param>
        /// <param name="itemEnumVal">The item in the enum list the ItemName comes from.</param>
        /// <param name="moduleName">Recommended but optional.</param>
        /// <param name="middleName">Optional.</param>
        /// <param name="bIsBitField">Indicates if the name being created is a BitField name.</param>
        /// <returns></returns>
        public static string NameCreator( Type itemEnumType,
                                          int itemEnumVal,
                                          string moduleName,
                                          string middleName,
                                          Boolean bIsBitField )
        {
            string itemName = Enum.GetName( itemEnumType, itemEnumVal );

            return NameCreator( itemName, moduleName, middleName, bIsBitField );
        }

        /// <summary>
        /// creates a register or BitField Name of "moduleName_middleName_ItemName"
        /// </summary>
        /// <param name="itemName">The name of the item (typically register or buffer).</param>
        /// <param name="moduleName">Recommended but optional.</param>
        /// <param name="middleName">Optional.</param>
        /// <param name="bIsBitField">Indicates if the name being created is a BitField name.</param>
        /// <returns></returns>
        public static string NameCreator( string itemName,
                                          string moduleName,
                                          string middleName,
                                          Boolean bIsBitField )
        {
            string fullName = string.Empty;

            if( moduleName != string.Empty )
            {
                fullName = moduleName + "_";
            }

            if( middleName != string.Empty )
            {
                if( bIsBitField )
                {
                    fullName += middleName + ":";
                }
                else
                {
                    fullName += middleName + "_";
                }
            }

            fullName += itemName;

            return fullName;
        }

        /// <summary>
        /// Due to Obfuscation, class names cannot be used with reflection to instantiate a register class (obfuscation
        /// results in a different class name on built/deployed systems than development systems, e.g. b3 vs.
        /// InstrumentDriver.Core.Register.Reg32).  Hence the "name" of the register type is the key in a dictionary
        /// to find the type.  The dictionary is populated via calls to RegFactory.RegisterType() which is typically
        /// performed by a static method, RegisterTypeWithFactory, in each register class.  RegFactory will register
        /// the "well-known" register types (i.e. those in ModularCore assembly/project) ... the developer of new
        /// register types must ensure those types include RegFactory.RegisterType()
        /// </summary>
        /// <param name="key">case-insensitive key to find a register type</param>
        /// <param name="registerType"></param>
        public static void RegisterType( string key, Type registerType )
        {
            lock( mRegisterTypes )
            {
                mRegisterTypes[ key ] = registerType;
            }
        }

        #endregion factory support and implementation

        #region factory methods to create Registers

        /// <summary>
        /// Creates an array of registers from a RegDef array. 
        /// </summary>
        /// <param name="regDefArray">This array of structs defines each reg to create.</param>
        /// <param name="regEnumType">The enumerated type naming each register to create.
        /// Each RegDef struct has a value representing an enum
        /// in this list.  The specified enum will from this list will become the RegName. </param>
        /// <param name="driver">Driver to write the reg out over PCIe bus. If null, create and use a RAM driver.</param>
        /// <param name="moduleName">Name of module the reg belongs to.</param>
        /// <returns>An array containing the created registers, indexed by the enums in regEnumType</returns>
        public IRegister[] CreateRegArray( ICollection <RegDef> regDefArray,
                                           Type regEnumType,
                                           IRegDriver driver,
                                           string moduleName )
        {
            return CreateRegArray( regDefArray, regEnumType, driver, moduleName, string.Empty, null );
        }


        public IRegister[] CreateRegArray( ICollection <RegDef> regDefArray, Type regEnumType )
        {
            return CreateRegArray( regDefArray, regEnumType, mRegDriver, mModuleName, string.Empty, null );
        }

        public IRegister[] CreateRegArray( ICollection <RegDef> regDefArray, Type regEnumType, object[] args )
        {
            return CreateRegArray( regDefArray, regEnumType, mRegDriver, mModuleName, string.Empty, args );
        }

        /// <summary>
        /// Creates an array of registers from a RegDef array. 
        /// </summary>
        /// <param name="regDefArray">This array of structs defines each reg to create</param>
        /// <param name="regEnumType">Each RegDef struct has a value representing an enum
        /// in this list.  The specified enum will from this list will become the RegName. </param>
        /// <param name="driver">Driver to write the reg out over PCIe bus.</param>
        /// <param name="moduleName">Name of module reg belongs to.</param>
        /// <param name="optionalMidName">Optional Name between module and reg name.</param>
        /// <returns>An array containing the created registers, indexed by the enums in regEnumType</returns>
        public IRegister[] CreateRegArray( ICollection <RegDef> regDefArray,
                                           Type regEnumType,
                                           IRegDriver driver,
                                           string moduleName,
                                           string optionalMidName )
        {
            return CreateRegArray( regDefArray, regEnumType, driver, moduleName, optionalMidName, null );
        }

        public Buffer32 CreateBuffer( Type enumType, int enumIndex, int startAddr, int size )
        {
            string name = NameCreator( enumType, enumIndex, mModuleName, "Buffer", false );
            return new Buffer32( name, startAddr, size, mRegDriver );
        }

        /// <summary>
        /// Creates an array of registers from a RegDef array. 
        /// </summary>
        /// <param name="regDefArray">This array of structs defines each reg to create</param>
        /// <param name="regEnumType">Each RegDef struct has a value representing an enum
        /// in this list.  The specified enum will from this list will become the RegName. </param>
        /// <param name="driver">Driver to write the reg out over PCIe bus.</param>
        /// <param name="moduleName">Name of module reg belongs to.</param>
        /// <param name="optionalMidName">Optional Name between module and reg name.</param>
        /// <param name="args">arbitrary array of objects for use by the delegate ... normally
        /// used to specify register-type specific arguments</param>
        /// <returns>An array containing the created registers, indexed by the enums in regEnumType</returns>
        public IRegister[] CreateRegArray( ICollection <RegDef> regDefArray,
                                           Type regEnumType,
                                           IRegDriver driver,
                                           string moduleName,
                                           string optionalMidName,
                                           object[] args )
        {
            int numRegsToCreate = Enum.GetNames( regEnumType ).Length;

            // Scan the enums (register name enum's) to look for the largest enumerated value
            // then compare this against the length of the enum array.  This should prevent
            // index out of bounds exceptions if someone doesn't override one of the "base" 
            // enumerated values and then extends the list of enum values.
            int largestEnum = 0;
            foreach( RegDef regDef in regDefArray )
            {
                largestEnum = Math.Max( largestEnum, regDef.nameEnum );
            }

            numRegsToCreate = Math.Max( numRegsToCreate, largestEnum + 1 );

            IRegister[] regArray = new IRegister[numRegsToCreate];

            // Now scan the register definition array and create each register and add them to the return array.
            foreach( RegDef regDef in regDefArray )
            {
                // Allow conditional inclusion/exclusion base on module flags
                if( Evaluate( regDef.Condition ) )
                {
                    string regName = NameCreator( regEnumType, regDef.nameEnum, moduleName, optionalMidName, false );

                    IRegister newReg = ConstructReg( regName, regDef, driver, mBaseAddr, args );

                    regArray[ regDef.nameEnum ] = newReg;
                }
            }

            return regArray;
        }

        /// <summary>
        /// **** EXPERIMENTAL AND NOT FINISHED ****
        /// Construct registers and bitfields from a definition read from a Stream (e.g. a file).
        /// 
        /// Format of configuration/stream:
        /// 
        /// // comment
        /// # comment
        /// [GroupName]
        /// [BAR, n]
        /// RegName, Offset, RegType, BitfieldType, Flags [,Objects]* [,BitFields]*]
        /// [BitFieldName, StartBit, Width]
        /// 
        /// where
        ///    GroupName=    arbitrary text (the name used with RegManager.AddGroup())
        ///    BAR=          selects which register driver is used (effectively, which BAR)
        ///    RegName=      arbitrary text (the name used with RegManager.GetRegister())
        ///    Offset=       BAR offset for memory mapped registers, typically device register address for
        ///                  others types. Decimal or Hex (prefix with '0x')
        ///    RegType=      maps to a registered type (see RegFactory.RegisterType()), case-insensitive. Typically Reg32 or Reg64.
        ///    BitFieldType  maps to existing bitfield type (i.e. Type.GetType(BitFieldType) ), may be "null"
        ///    Flags=        RegType.Value [|RegType.Value[|RegType.Value...]]], e.g.  Buffer|RO
        ///    Objects=      Group.RegisterName[.FieldName] ... passed to ConstructRegDelegate in args[]
        ///    BitFields=    Name,StartBit,Width
        ///
        /// If GroupName is specified, all registers following the GroupName up to the next GroupName or end of file
        /// are automatically added to that group AND NOT RETURNED.
        ///  
        /// The returned array will contain the registers in the order the definition appears in the
        /// stream.
        /// 
        /// NOTE: comments may appear on any line.  E.g.  GroupName # this is the group name
        /// 
        /// NOTE: anywhere "," is shown in the syntax you can substitute space (" ") or equals ("=")
        /// 
        /// NOTE: either place ALL bitfields on the same line as the register definition OR one bitfield per line following the register definition
        /// 
        /// Example:
        ///    TestGroup
        ///    Bar=4
        ///    MyReg 0x4000 Reg32 null RW
        ///       LSW 0,16
        ///       MSW 16,16
        /// 
        /// Will result in a single register in group 'TestGroup' named 'MyReg' that read/writes 0x4000 in BAR4. The
        /// register will have two bitfields for bits 0..15 (LSW) and 16..31 (MSW)
        /// 
        /// See RegisterTestSuite.TestRuntimeRegisterFactory for example usage
        /// 
        /// For more information see $/MPO/Core/trunk/software/docs/Registers.docx
        /// </summary>
        /// <param name="input"></param>
        /// <param name="regManager"></param>
        /// <returns></returns>
        public IRegister[] CreateRegArray( Stream input, IRegManager regManager )
        {
            StreamReader reader = new StreamReader( input );
            List <IRegister> registers = new List <IRegister>();
            int nameIndex = 0;
            string groupName = string.Empty;
            // initialize the register driver to the default (in case 'BAR' is not specified)
            IRegDriver regDriver = mRegDriver;
            char[] delimiters = { ',', '=', ' ' };
            IRegister previousRegister = null;
            List<IBitField> fields = new List <IBitField>();

            while( ! reader.EndOfStream )
            {
                // Ignore blank lines and comments...
                string line = reader.ReadLine();
                if( line != null )
                {
                    line = Regex.Replace( line, "(#|//).*$", "", RegexOptions.CultureInvariant );
                    line = line.Trim();
                }
                if( string.IsNullOrEmpty( line ) ||
                    line.StartsWith( "//" ) ||
                    line.StartsWith( "#" ) )
                {
                    continue;
                }

                // Split line into tokens and trim them...
                string[] tokens = SplitAndTrim( line, delimiters );

                // NOTE: checking for bitfields MUST be done before "finishing" the previous register
                // 3 tokens:  "field,startBit,fieldWidth
                if( tokens.Length == 3 )
                {
                    if( previousRegister == null )
                    {
                        throw new InvalidParameterException(
                            string.Format( "No 'open' register for field definition. Failed at: '{0}'", line ) );
                    }
                    if( previousRegister.Fields != null )
                    {
                        throw new InvalidParameterException(
                            string.Format( "Fields already defined for register '{0}'. Failed at: '{1}'", previousRegister.Name, line ) );
                    }
                    ParseBitField( fields, tokens, 0, previousRegister, line );
                    continue;
                }

                // Finish the previous register definition, if any (merge bitfields).  This must be done before anything
                // that might define a new register (hence do this before checking for group, bar, register....)
                FinishPreviousRegister( previousRegister, fields );
                previousRegister = null;

                // If a single token, this is a group name...
                if( tokens.Length == 1 )
                {
                    // Any existing registers go into the existing group (if defined)
                    if( ! string.IsNullOrEmpty( groupName ) && registers.Count > 0 )
                    {
                        regManager.AddGroup( groupName, registers.ToArray() );
                        registers.Clear();
                    }
                    groupName = tokens[ 0 ];
                    continue;
                }

                // 2 tokens: "BAR n"
                if( tokens.Length == 2 &&
                    tokens[ 0 ].Equals( "bar", StringComparison.InvariantCultureIgnoreCase ) )
                {
                    try
                    {
                        int barIndex = Int32.Parse( tokens[ 1 ] );
                        if( mRegDrivers == null || barIndex < 0 || barIndex >= mRegDrivers.Length )
                        {
                            throw new InvalidParameterException(
                                string.Format( "Bar index ({0}) exceeds bounds of register drivers.", barIndex ) );
                        }
                        regDriver = mRegDrivers[ barIndex ];
                        if( regDriver == null )
                        {
                            throw new InvalidParameterException(
                                string.Format( "Bar index ({0}) selects a null register driver. Failed at: '{1}'", barIndex, line ) );
                        }
                    }
                    catch
                    {
                        throw new InvalidParameterException(
                            string.Format( "Invalid argument for bar:  '{0}'", tokens[ 1 ] ) );
                    }
                    continue;
                }

                // We need a minimum of 5 tokens (for registers without bitfields)
                if( tokens.Length < 5 )
                {
                    throw new InvalidParameterException(
                        string.Format( "Need 1 field for 'group;, 2 fields for 'bar', 3 fields for bitfield, and at least 5 fields for register definition (e.g. RegName, 0x100, Reg32, null, RW). Failed at: '{0}'", line ) );
                }

                // Fixed indices into tokens...
                const int REGISTER_NAME_INDEX = 0;
                const int REGISTER_OFFSET_INDEX = 1;
                const int REGISTER_TYPE_INDEX = 2;
                const int BITFIELD_TYPE_INDEX = 3;
                const int REGISTER_FLAGS_INDEX = 4;
                const int FIRST_OBJECT_INDEX = 5;

                // Construct the register instance...
                IRegister register;
                try
                {
                    // Find the register type and ConstructReg method
                    MethodInfo constructReg = ParseRegisterType( tokens[ REGISTER_TYPE_INDEX ], line );

                    // Any "objects" (to be used in CreateReg call for 'args')?
                    object[] args = ParseObjects( regManager, tokens, FIRST_OBJECT_INDEX, line );

                    // Various other definitions...
                    RegDef definition = ParseRegDef( nameIndex,
                                                     tokens[ REGISTER_OFFSET_INDEX ],
                                                     tokens[ REGISTER_FLAGS_INDEX ],
                                                     tokens[ BITFIELD_TYPE_INDEX ],
                                                     line );

                    register = (IRegister)constructReg.Invoke( null,
                                                               new object[]
                                                                   {
                                                                       tokens[ REGISTER_NAME_INDEX ],
                                                                       definition,
                                                                       regDriver,
                                                                       mBaseAddr,
                                                                       args
                                                                   } );

                    // Parse and add bitfields... The first bitfield token is after the last object
                    ParseBitFields( register, tokens, FIRST_OBJECT_INDEX + args.Length, line );
                }
                catch( Exception ex )
                {
                    throw new Exception(
                        string.Format( "Could not create register '{0}'. Failed at: '{1}'\n{2}",
                                       tokens[ REGISTER_NAME_INDEX ],
                                       line,
                                       ex.InnerException.Message ) );
                }

                // And, finally, include the register in the list/array...
                registers.Add( register );
                previousRegister = register;
                nameIndex++;
            }

            // Finish the previous register definition, if any (merge bitfields).
            FinishPreviousRegister( previousRegister, fields );

            // If a group was defined, put the registers in it (and don't return them)
            if( ! string.IsNullOrEmpty( groupName ) && registers.Count > 0 )
            {
                regManager.AddGroup( groupName, registers.ToArray() );
                registers.Clear();
            }

            return registers.ToArray();
        }

        private void FinishPreviousRegister( IRegister register, IList <IBitField> bitfields )
        {
            if( register != null &&
                register.Fields == null &&
                bitfields.Count > 0 )
            {
                AddBitFieldsToRegister( bitfields, register );
            }
            // always clear the bitfields to make room for the next register's bitfields
            bitfields.Clear();
        }

        #endregion factory methods to create Registers

        #region factory methods to create BitFields

        /// <summary>
        /// Creates a BitField and inserts it into the containing register.
        /// </summary>
        /// <param name="bfd">structure defining the BitField.</param>
        /// <param name="containingReg">Register this BitField belongs to.</param>
        /// <returns>The created BitField</returns>
        private IBitField CreateBitField( BitFieldDefBase bfd,
                                          IRegister containingReg )
        {
            IBitField newBitField;

            // create the bit field name. 
            string bitFieldName = NameCreator( containingReg.BFType,
                                               bfd.nameEnum,
                                               string.Empty,
                                               containingReg.Name,
                                               true );

            if( ConstructField != null )
            {
                newBitField = ConstructField( bitFieldName,
                                              bfd.nameEnum,
                                              bfd.startBit,
                                              bfd.width,
                                              containingReg,
                                              bfd.Args );
            }
            else if( containingReg.SizeInBytes == sizeof( long ) )
            {
                if( containingReg.IsCommand || containingReg.IsEvent )
                {
                    // Command/event registers get command/event bits fields (changes how Value/Apply and Write work)
                    newBitField = new CommandField64( bitFieldName,
                                                      bfd.startBit,
                                                      bfd.width,
                                                      containingReg );
                }
                else
                {
                    // Non-command/event registers get "normal" bit fields
                    newBitField = new RegField64( bitFieldName,
                                                  bfd.startBit,
                                                  bfd.width,
                                                  containingReg );
                }
            }
            else if( containingReg.SizeInBytes == sizeof( int ) )
            {
                if( containingReg.IsCommand || containingReg.IsEvent )
                {
                    // Command/event registers get command/event bits fields (changes how Value/Apply and Write work)
                    newBitField = new CommandField32( bitFieldName,
                                                      bfd.startBit,
                                                      bfd.width,
                                                      containingReg );
                }
                else
                {
                    // Non-command/event registers get "normal" bit fields
                    newBitField = new RegField32( bitFieldName,
                                                  bfd.startBit,
                                                  bfd.width,
                                                  containingReg );
                }
            }
            else
            {
                throw new ApplicationException( "Invalid Reg Array Type" );
            }

            return newBitField;
        }

        /// <summary>
        /// Creates BitFields from an array of BitFieldDef's, then inserts them into the 
        /// register that contains them as well as inserts them into a bit field server.
        /// </summary>
        /// <param name="bitFieldDefArray">Array of BitFieldDef structs, one for each bit field.</param>
        /// <param name="containingRegArray">Reg array containing the registers that contain
        /// all bitfields being defined in the BitFieldDef[].</param>
        /// <param name="moduleName">Name of containing module.</param>
        /// <remarks>
        /// This method is intended to be called when the BitFields defined in the
        /// BitFieldDef[] are each destined for a single register, which is the normal case.
        /// </remarks>
        /// <remarks>
        /// This method performs some hidden filtering ... if BitFieldDef includes a definition
        /// for FilterKey and FilterValue then the BitField will only be created IF
        ///       Regex.IsMatch( module.Service.GetValue( bfd.FilterKey ), bfd.FilterValue )
        /// </remarks>
        public void CreateBitFields( ICollection <BitFieldDef> bitFieldDefArray,
                                     IRegister[] containingRegArray,
                                     string moduleName )
        {
            foreach( BitFieldDef bfd in bitFieldDefArray )
            {
                // Allow conditional inclusion/exclusion base on module flags
                if( Evaluate( bfd.Condition ) )
                {
                    // Two cases:
                    //   bfd.BFType == null: create a BitField for the register identified by regEnum
                    //   bfd.BFType != null: create a BitField for ALL registers with IRegister.BFType == bfd.BFType
                    if( bfd.BFType == null )
                    {
                        IRegister containingReg = containingRegArray[ bfd.RegEnum ];
                        if( containingReg != null )
                        {
                            IBitField newBitField = CreateBitField( bfd, containingReg );
                            containingReg.AddField( newBitField ); // add newly created BitField to it's reg.
                        }
                    }
                    else
                    {
                        foreach( IRegister containingReg in containingRegArray )
                        {
                            if( containingReg != null && ReferenceEquals( containingReg.BFType, bfd.BFType ) )
                            {
                                IBitField newBitField = CreateBitField( bfd, containingReg );
                                containingReg.AddField( newBitField ); // add newly created BitField to it's reg.
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Creates BitFields from an array of BitFieldDef's, then inserts them into the 
        /// register that contains them as well as inserts them into a bit field server.
        /// </summary>
        /// <param name="bitFieldDefArray">Array of BitFieldDef structs, one for each bit field.</param>
        /// <param name="containingRegArray">Reg array containing the registers that contain
        /// all bitfields being defined in the BitFieldDef[].</param>
        /// <param name="moduleName">Name of containing module.</param>
        /// <param name="optionalMidName">Optional name between board and BitField. (IGNORED!!!)</param>
        /// <remarks>This method is intended to be called when the BitFields defined in the
        /// BitFieldDef[] are each destined for a single register, which is the normal case.</remarks>
        public void CreateBitFields( BitFieldDef[] bitFieldDefArray,
                                     IRegister[] containingRegArray,
                                     string moduleName,
                                     string optionalMidName )
        {
            // NOTE: 'optionalMidName' is IGNORED
            CreateBitFields( bitFieldDefArray, containingRegArray, moduleName );
        }


        /// <summary>
        /// Given a common set of BitField defs, create the BitFields and insert them into
        /// each register in the register array. 
        /// </summary>
        /// <param name="bitFieldDefArray">Array of BitFieldDefBase structs, one for each bit field.</param>
        /// <param name="regArray">Reg array containing the registers that EACH will
        /// contain this identical set of BitField definitions.</param>
        /// <param name="moduleName">Name of containing module.</param>
        /// <param name="optionalMidName">Optional name between board and BitField.</param>
        /// <remarks>This method is only used in the special case when an entire array of 
        /// registers, each have the exact same bitfield defs which are defined in the
        /// BitFieldDefBase[] array.  This is probably a one of a kind scenario.</remarks>
        public void CreateBitFields( BitFieldDefBase[] bitFieldDefArray,
                                     IRegister[] regArray,
                                     string moduleName,
                                     string optionalMidName )
        {
            foreach( IRegister reg in regArray )
            {
                CreateBitFields( bitFieldDefArray, reg, moduleName, optionalMidName );
            }
        }


        /// <summary>
        /// Given a common set of BitField defs, create the BitFields and insert them into
        /// each register in the register array. 
        /// </summary>
        /// <param name="bitFieldDefArray">Array of BitFieldDefBase structs, one for each bit field.</param>
        /// <param name="regArray">Reg array containing the registers that EACH will
        /// contain this identical set of BitField definitions.</param>
        /// <param name="moduleName">Name of containing module.</param>
        /// <param name="optionalMidName">Optional name between board and BitField.</param>
        /// <param name="startIndex">first index of reg to apply bit field to.</param> 
        /// <param name="endIndex">last index of reg to apply bit field to.</param>
        /// <remarks>This method is only used in the special case when an entire array of 
        /// registers, each have the exact same bitfield defs which are defined in the
        /// BitFieldDefBase[] array.  This is probably a one of a kind scenario.</remarks>
        public void CreateBitFields( BitFieldDefBase[] bitFieldDefArray,
                                     IRegister[] regArray,
                                     int startIndex,
                                     int endIndex,
                                     string moduleName,
                                     string optionalMidName )
        {
            for( int i = startIndex; i <= endIndex; i++ )
            {
                CreateBitFields( bitFieldDefArray, regArray[ i ], moduleName, optionalMidName );
            }
        }

        /// <summary>
        /// Applies bit fields to a single register. No type checking is done,
        /// the caller MUST know that the bit field types match that of the register.
        /// </summary>
        /// <remarks>This usage of this method is when multiple single registers (usually it
        /// is two registers) need to have the same bitfield defs.  The problem with the 
        /// primary CreateBitFields method is that the bit field defs in that method are
        /// mapped one definition per register.  So you would have to fill out duplicate
        /// bit field definitions and point them at different registers.  Not an efficient 
        /// use of typing as well as error prone and difficult to maintain.  This method
        /// allows us to type the BitFieldDefinitions just one and therefore they are easy
        /// to maintain.  The downside is that this method must be called once per register.
        /// </remarks>
        public void CreateBitFields( ICollection <BitFieldDefBase> bitFieldDefArray,
                                     IRegister reg,
                                     string moduleName,
                                     string optionalMidName )
        {
            foreach( BitFieldDefBase bfd in bitFieldDefArray )
            {
                // Allow conditional inclusion/exclusion base on module flags
                if( Evaluate( bfd.Condition ) )
                {
                    IBitField newBitField = CreateBitField( bfd, reg );
                    reg.AddField( newBitField ); // add newly created BitField to it's reg.
                }
            }
        }

        #endregion factory methods to create BitFields

        #region support methods

        protected string[] SplitAndTrim( string line, char separator )
        {
            return SplitAndTrim( line, new [] { separator } );
        }

        protected string[] SplitAndTrim( string line, char[] separators )
        {
            string[] tokens = line.Split( separators, StringSplitOptions.RemoveEmptyEntries );
            for( int j = 0; j < tokens.Length; j++ )
            {
                tokens[ j ] = tokens[ j ].Trim();
            }
            return tokens;
        }

        /// <summary>
        /// Parse the "objects" specified for dynamically creating registers... The returned
        /// array is typically used as the 'args' parameter to ConstructRegDelegate.
        /// The format is:
        /// 
        ///     null|Group.Register[.Field]
        /// 
        /// The register will be found using IRegManager.GetRegister(Group, Register)
        /// 
        /// If the field is specified, the field will be found by iterating over the fields in
        /// the register and comparing Field with Register.GetField(x).ShortName
        /// 
        /// The returned array will contain the objects in the order they are listed in tokens.
        /// 
        /// This method will consume tokens starting at firstIndex and stop at the index where
        /// either index >= tokens.Length or the token does not contain '.'
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="tokens"></param>
        /// <param name="firstIndex"></param>
        /// <param name="line">The input line that supplied tokens ... used for generating error messages</param>
        /// <returns></returns>
        protected object[] ParseObjects( IRegManager manager, string[] tokens, int firstIndex, string line )
        {
            List <object> objects = new List <object>();

            int index = firstIndex;
            while( index < tokens.Length )
            {
                // All object definitions are in the form of "Group.Register[.Field]" with 1 exception ("null")...
                string objectName = tokens[ index++ ];
                if( objectName.Equals( "null", StringComparison.InvariantCultureIgnoreCase ) )
                {
                    objects.Add( null );
                    continue;
                }
                if( !objectName.Contains( "." ) )
                {
                    break;
                }
                string[] names = SplitAndTrim( objectName, '.' );
                if( names.Length < 2 || names.Length > 3 )
                {
                    throw new InvalidParameterException(
                        string.Format( "Unrecognized 'object' format '{0}'. Failed at {1}",
                                       objectName,
                                       line ) );
                }
                IRegister register = manager.GetRegister( names[ 0 ], names[ 1 ] );
                if( register == null )
                {
                    throw new InvalidParameterException( string.Format( "Could not find register '{0}'. Failed at {1}",
                                                                        objectName,
                                                                        line ) );
                }

                object item = null;
                if( names.Length == 2 )
                {
                    item = register;
                    objects.Add( register );
                }
                else
                {
                    for( int j = register.FirstBF; j <= register.LastBF; j++ )
                    {
                        IBitField field = register.GetField( (uint)j );
                        if( field.ShortName.Equals( names[ 2 ], StringComparison.InvariantCultureIgnoreCase ) )
                        {
                            item = field;
                            break;
                        }
                    }
                }
                if( item == null )
                {
                    throw new InvalidParameterException( string.Format( "Could not find 'object' '{0}'",
                                                                        objectName ) );
                }
                objects.Add( item );
            }

            return objects.ToArray();
        }

        /// <summary>
        /// Create the RegDef corresponding to the tokens from the register configuration (line).
        /// 
        /// Unlike register type in ParseRegisterType, bitfieldTypeToken does specify a class name and this method
        /// uses reflection to find the type. Obfuscation doesn't interfere because bitfield enums should not be
        /// obfuscated since register diagnostics rely on reflection to populate the GUI controls.
        /// </summary>
        /// <param name="nameIndex">the index this register will be positioned in the register group array</param>
        /// <param name="offsetToken">integer offset.  Typically BAR offset or register address</param>
        /// <param name="flagToken">one or more RegType enumerations (combine with '|')</param>
        /// <param name="bitfieldTypeToken">type identifier for bit field, may be "null"</param>
        /// <param name="line">The input line that supplied tokens ... used for generating error messages</param>
        /// <returns></returns>
        protected RegDef ParseRegDef( int nameIndex,
                                      string offsetToken,
                                      string flagToken,
                                      string bitfieldTypeToken,
                                      string line )
        {
            // Register offset...
            int offset;
            if( offsetToken.StartsWith( "0x", StringComparison.CurrentCultureIgnoreCase ) )
            {
                if(
                    ! int.TryParse( offsetToken.Substring( 2 ),
                                    NumberStyles.HexNumber,
                                    CultureInfo.InvariantCulture,
                                    out offset ) )
                {
                    throw new InvalidParameterException(
                        string.Format(
                            "Register offset '{0}' must be an integer (may be hex if starts with '0x'). Failed at: '{1}'",
                            offsetToken,
                            line ) );
                }
            }
            else if( ! int.TryParse( offsetToken, out offset ) )
            {
                throw new InvalidParameterException(
                    string.Format(
                        "Register offset '{0}' must be an integer (may be hex if starts with '0x'). Failed at: '{1}'",
                        offsetToken,
                        line ) );
            }

            // Bitfield type...
            Type bitfieldType = null;

            // Parse flags..
            short registerFlags = 0;
            string[] flags = SplitAndTrim( flagToken, '|' );
            foreach( var flag in flags )
            {
                try
                {
                    RegType type = (RegType)Enum.Parse( typeof( RegType ), flag, true );
                    registerFlags |= (short)type;
                }
                catch( Exception )
                {
                    throw new Exception(
                        string.Format( "Register flag '{0}' is not recognized. Failed at: '{1}'",
                                       flag,
                                       line ) );
                }
            }
            return new RegDef( nameIndex, offset, bitfieldType, (RegType)registerFlags );
        }

        /// <summary>
        /// Parse the register type (e.g. Reg32, AddrDataReg32, etc.) and return the MethodInfo
        /// for ConstructRegDelegate.
        /// 
        /// Due to Obfuscation, 'token' cannot be used with reflection to instantiate the register class (obfuscation
        /// results in a different class name on built/deployed systems than development systems, e.g. b3 vs.
        /// InstrumentDriver.Core.Register.Reg32).  Hence the "name" of the register type is the key in a dictionary
        /// to find the type.  The dictionary is populated via calls to RegFactory.RegisterType() which is typically
        /// performed by a static method, RegisterTypeWithFactory, in each register class.  RegFactory will register
        /// the "well-known" register types (i.e. those in ModularCore assembly/project) ... the developer of new
        /// register types must ensure those types include RegFactory.RegisterType()
        /// </summary>
        /// <param name="token"></param>
        /// <param name="line">The input line that supplied tokens ... used for generating error messages</param>
        /// <returns></returns>
        protected MethodInfo ParseRegisterType( string token, string line )
        {
            // Identify the types
            Type registerType = null;
            lock( mRegisterTypes )
            {
                if( mRegisterTypes.ContainsKey( token ) )
                {
                    registerType = mRegisterTypes[ token ];
                }
                // Don't try reflection -- that will break when the code is obfuscated
            }

            // The register type must exist and must have 'ConstructReg'
            if( registerType == null )
            {
                throw new InvalidParameterException(
                    string.Format( "Register type '{0}' doesn't exist. Failed at: '{1}'",
                                   token,
                                   line ) );
            }

            // We will look for 'ConstructReg' by name and by signature ... if the name is not
            // protected from obfuscation then the signature must be unique in the class
            MethodInfo constructReg = registerType.GetMethod( "ConstructReg" );
            if( constructReg == null )
            {
                // 'ConstructReg' was probably obfuscated ... search for it by signature, namely:
                //     public static IRegister ConstructReg( string, RegDef, IRegDriver, int, object[] )
                foreach( var methodInfo in registerType.GetMethods( BindingFlags.Static | BindingFlags.Public ) )
                {
                    // The method must 
                    // 1) have 5 arguments:  string, RegDef, IRegDriver, int, object[]
                    // 2) return a value: IRegister
                    ParameterInfo[] parameters = methodInfo.GetParameters();
                    if( methodInfo.ReturnParameter != null &&
                        parameters.Length == 5 &&
                        methodInfo.ReturnParameter.ParameterType == typeof( IRegister ) &&
                        parameters[ 0 ].ParameterType == typeof( string ) &&
                        parameters[ 1 ].ParameterType == typeof( RegDef ) &&
                        parameters[ 2 ].ParameterType == typeof( IRegDriver ) &&
                        parameters[ 3 ].ParameterType == typeof( int ) &&
                        parameters[ 4 ].ParameterType == typeof( object[] ) )
                    {
                        constructReg = methodInfo;
                        break;
                    }
                }
                if( constructReg == null )
                {
                    throw new InvalidParameterException(
                        string.Format( "Register type '{0}' doesn't implement 'ConstructReg'. Failed at: '{1}'",
                            token,
                            line ) );
                }
            }

            return constructReg;
        }

        /// <summary>
        /// Parse the bitfields specified for dynamically creating registers...
        /// 
        ///     name, start, size
        /// 
        /// where
        ///   name .... is the name of the bitfield.  May correspond to an enumeration of RegDef.BFenum
        ///   start ... is the start bit of the field (integer)
        ///   size .... is the width of the field, in bits (integer)
        /// 
        /// NOTE: the implementation is a bit messy because we don't have a BitField interface (yet)
        /// </summary>
        /// <param name="register">the register the bitfields will be added to</param>
        /// <param name="tokens">tokenized register/bitfields definition</param>
        /// <param name="first">index of first possible bitfield triplet</param>
        /// <param name="line">The input line that supplied tokens ... used for generating error messages</param>
        /// <returns></returns>
        protected void ParseBitFields( IRegister register, string[] tokens, int first, string line )
        {
            List <IBitField> bitfields = new List <IBitField>();

            // Each bitfield requires 3 tokens...
            int index = first;
            while( index + 2 < tokens.Length )
            {
                ParseBitField( bitfields, tokens, index, register, line );

                index += 3;
            }

            // Finally, add bit fields to register...
            AddBitFieldsToRegister( bitfields, register );
        }

        /// <summary>
        /// Parse a single bitfield (3 tokens) starting at tokens[index] and add it to bitfields.  The format is
        /// 
        ///     name, start, size
        /// 
        /// where
        ///   name .... is the name of the bitfield.  May correspond to an enumeration of RegDef.BFenum
        ///   start ... is the zero based start bit of the field (integer)
        ///   size .... is the width of the field, in bits (integer)
        /// </summary>
        /// <param name="bitfields"></param>
        /// <param name="tokens"></param>
        /// <param name="index"></param>
        /// <param name="register"></param>
        /// <param name="line"></param>
        protected void ParseBitField( IList <IBitField> bitfields, string[] tokens, int index, IRegister register, string line )
        {
            // Extract name, start bit, # bits
            string name = tokens[ index ];
            int start;
            int size;

            if( ! int.TryParse( tokens[ index + 1 ], out start ) )
            {
                throw new InvalidParameterException(
                    string.Format( "Invalid start bit '{0}'. Failed at: '{1}'",
                                    tokens[ index + 1 ],
                                    line ) );
            }

            if( ! int.TryParse( tokens[ index + 2 ], out size ) )
            {
                throw new InvalidParameterException(
                    string.Format( "Invalid bit size '{0}'. Failed at: '{1}'",
                                    tokens[ index + 2 ],
                                    line ) );
            }

            IBitField bitfield;
            if( register.SizeInBytes == sizeof( int ) )
            {
                if( register.IsCommand || register.IsEvent )
                {
                    // Command/event registers get command/event bits fields (changes how Value/Apply and Write work)
                    bitfield = new CommandField32( name, start, size, register );
                }
                else
                {
                    // Non-command/event registers get "normal" bit fields
                    bitfield = new RegField32( name, start, size, register );
                }
            }
            else if( register.SizeInBytes == sizeof( long ) )
            {
                if( register.IsCommand || register.IsEvent )
                {
                    // Command/event registers get command/event bits fields (changes how Value/Apply and Write work)
                    bitfield = new CommandField64( name, start, size, register );
                }
                else
                {
                    // Non-command/event registers get "normal" bit fields
                    bitfield = new RegField64( name, start, size, register );
                }
            }
            else
            {
                throw new InvalidParameterException(
                    string.Format(
                        "Register type doesn't support RegField32 or RegField64 bitfields. Failed at: '{0}'",
                        line ) );
            }
            bitfields.Add( bitfield );
        }

        /// <summary>
        /// "Repackage" the IBitFields in 'bitfields' and add them to register.
        /// </summary>
        /// <param name="bitfields">the bitfields to add</param>
        /// <param name="register">the register to receive the bitfields</param>
        protected void AddBitFieldsToRegister( IList <IBitField> bitfields, IRegister register )
        {
            if( bitfields.Count > 0 )
            {
                // TODO CORE: if the definition included a BitField type/enum (register.BFType), need to arrange the array per the enum
                int firstBF = 0;
                int numBF = bitfields.Count;
                int arraySize = bitfields.Count;

                IBitField[] array = new IBitField[arraySize];
                for( int j = 0; j < numBF; j++ )
                {
                    array[ firstBF + j ] = bitfields[ j ];
                }
                register.Fields = array;
            }
        }

        /// <summary>
        /// Evaluate 'condition' and return if the expression is true or false.  If condition is null or
        /// empty, returns true.
        /// </summary>
        /// <remarks>
        /// As of 16-Sept-2015 Condition takes the form of {Key}{op}{Value} where
        /// 
        /// Key ..... string value passed to IInstrument.Service.GetValue to retrieve the value of interest
        /// op ...... normal comparision operators (==, !=, etc.) or 'match'/'nomatch' which performs Regex.IsMatch( GetValue(Key), Value )
        /// Value ... the comparison value.
        /// 
        /// All of the comparison operators will convert the operands to integer values 
        /// </remarks>
        private bool Evaluate( string condition )
        {
            if( string.IsNullOrEmpty( condition ) )
            {
                return true;
            }
            
            // Split the condition
            char[] delimiters = { '=', '!', '<', '>', ' ' };
            string[] tokens = condition.Split( delimiters, StringSplitOptions.RemoveEmptyEntries );
            if( tokens.Length < 2 )
            {
                throw new InternalApplicationException( string.Format( "Invalid condition ('{0}')", condition ) );
            }

            string key = tokens[ 0 ];
            string arg1 = mModule.Service.GetValue( key );
            string arg2 = tokens[ tokens.Length - 1 ];
            // The operator may have been split into multiple tokens
            string op = condition.Substring( key.Length, condition.Length - key.Length - arg2.Length ).Trim();

            // Try to convert parameters (failure to convert is not fatal)
            int value1;
            int value2;
            bool isNumeric = Int32.TryParse( arg1, out value1 );
            isNumeric &= Int32.TryParse( arg2, out value2 );

            switch( op.ToLowerInvariant() )
            {
                case "match":
                    return Regex.IsMatch( arg1, arg2, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase );

                case "notmatch":
                    return ! Regex.IsMatch( arg1, arg2, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase );

                case "=":
                case "==":
                    return isNumeric ? value1 == value2 : arg1.Equals( arg2, StringComparison.InvariantCultureIgnoreCase );

                case "!=":
                case "<>":
                    return isNumeric ? value1 != value2 : ! arg1.Equals( arg2, StringComparison.InvariantCultureIgnoreCase );

                case ">":
                    if( isNumeric )
                    {
                        return value1 > value2;
                    }
                    throw new InternalApplicationException( string.Format( "Cannot use '>' with non-numeric ({0}>{1})",
                        arg1,
                        arg2 ) );

                case ">=":
                    if( isNumeric )
                    {
                        return value1 >= value2;
                    }
                    throw new InternalApplicationException( string.Format( "Cannot use '>=' with non-numeric ({0}>={1})",
                        arg1,
                        arg2 ) );

                case "<":
                    if( isNumeric )
                    {
                        return value1 < value2;
                    }
                    throw new InternalApplicationException( string.Format( "Cannot use '<' with non-numeric ({0}<{1})",
                        arg1,
                        arg2 ) );

                case "<=":
                    if( isNumeric )
                    {
                        return value1 <= value2;
                    }
                    throw new InternalApplicationException( string.Format( "Cannot use '<=' with non-numeric ({0}<={1})",
                        arg1,
                        arg2 ) );

                default:
                    throw new InternalApplicationException( string.Format(
                        "Unrecognized operation in condition ('{0}')",
                        condition ) );
            }
        }
        #endregion support methods
    }
}
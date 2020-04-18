using System;
using InstrumentDriver.Core.Utility.Log;

namespace InstrumentDriver.Core.Register
{
    /// <summary>
    /// SimulatedBitField is an IBitField implementation that is typically backed by software logic instead
    /// of a gateware register (although a custom specialization could certainly involve registers and other
    /// IBitField instances).
    /// </summary>
    /// <remarks>
    /// An example use for SimulatedBitField would be to replace reading a register bit (which is relatively
    /// slow) with a timer, <see cref="InstrumentDriver.Core.Keystone.AlteraQuarusSpiSession"/>
    /// </remarks>
    public class SimulatedBitField : IBitField
    {
        public delegate int SimulatedBitFieldReadDelegate();

        public delegate void SimulatedBitFieldWriteDelegate( IRegDriver driver, int value );

        #region variables

        protected static ILogger mLogger = LogManager.RootLogger;
        protected readonly object mResource = new object();
        protected readonly SimulatedBitFieldReadDelegate mReadDelegate;
        protected readonly SimulatedBitFieldWriteDelegate mWriteDelegate;
        protected int mBitFieldValue;

        #endregion variables

        #region constructors

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="name">the name reported via IBitField.Name and IBitField.ShortName</param>
        /// <remarks>
        /// If readDelegate and writeDelegate are not supplied, the value of the bitfield is 
        /// simply the last value set (by Value or Write)
        /// </remarks>
        public SimulatedBitField( string name )
            : this( name, 0, 0, null, null )
        {
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="name">the name reported via IBitField.Name and IBitField.ShortName</param>
        /// <param name="readDelegate">optional read delegate used by Read()</param>
        /// <remarks>
        /// If readDelegate and writeDelegate are not supplied, the value of the bitfield is 
        /// simply the last value set (by Value or Write)
        /// </remarks>
        public SimulatedBitField( string name, SimulatedBitFieldReadDelegate readDelegate )
            : this( name, 0, 0, readDelegate, null )
        {
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="name">the name reported via IBitField.Name and IBitField.ShortName</param>
        /// <param name="readDelegate">optional read delegate used by Read()</param>
        /// <param name="writeDelegate">optional write delegate used by Write()</param>
        /// <remarks>
        /// If readDelegate and writeDelegate are not supplied, the value of the bitfield is 
        /// simply the last value set (by Value or Write)
        /// </remarks>
        public SimulatedBitField( string name,
            SimulatedBitFieldReadDelegate readDelegate,
            SimulatedBitFieldWriteDelegate writeDelegate )
            : this( name, 0, 0, readDelegate, writeDelegate )
        {
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="name">the name reported via IBitField.Name and IBitField.ShortName</param>
        /// <param name="startBit">start bit ... other than setting IBitField.StartBit, ignored by default implementation</param>
        /// <param name="endBit">end bit ... other than setting IBitField.EndBit, ignored by default implementation</param>
        /// <param name="readDelegate">optional read delegate used by Read()</param>
        /// <param name="writeDelegate">optional write delegate used by Write()</param>
        /// <remarks>
        /// If readDelegate and writeDelegate are not supplied, the value of the bitfield is 
        /// simply the last value set (by Value or Write)
        /// </remarks>
        public SimulatedBitField( string name,
            int startBit,
            int endBit,
            SimulatedBitFieldReadDelegate readDelegate,
            SimulatedBitFieldWriteDelegate writeDelegate )
        {
            Name = name;
            ShortName = name;
            StartBit = startBit;
            EndBit = endBit;
            Size = EndBit - StartBit + 1;
            Mask = ( ( 1 << Size ) - 1 ) << startBit;
            mReadDelegate = readDelegate;
            mWriteDelegate = writeDelegate;
        }

        #endregion constructors

        #region Implementation of IBitField

        /// <summary>
        /// The full name of the BitField -- normally this is the value specified to the
        /// constructor and is assumed to be in the form of {register name}:{bitfield name}
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// The "short name" of the BitField (just the BitField name sans the register name).
        /// Normally this is the portion of the (full) Name following ":".
        /// </summary>
        public string ShortName
        {
            get;
            private set;
        }

        /// <summary>
        /// The 'resource' the implementing register will lock before performing operations.  If a client
        /// needs to perform multi-field/register operations atomically, execute the code inside:
        /// 
        /// <code>
        ///    lock( IBitField.Resource )
        ///    {
        ///       // atomic code goes here
        ///    }
        /// </code>
        /// </summary>
        /// <remarks>
        /// Normally this is the IRegister.Resource value of the register that 'owns' this IBitField
        /// instance, e.g.   get { return mRegister.Resource; }
        /// </remarks>
        public object Resource
        {
            get
            {
                return mResource;
            }
        }

        /// <summary>
        /// Gets or Sets the cached latch value, does not access HW.  A set leaves the register
        /// marked dirty. Use Apply() to write dirty values to HW or use Write() to "directly"
        /// write value to HW.
        /// 
        /// Most implementations preserve all bits outside the current bitfield definition,
        /// but some implementations (CommandField32, CommandField64) clear all other bits.
        /// </summary>
        public virtual int Value
        {
            get
            {
                return ReadBitField();
            }
            set
            {
                WriteBitField( value );
            }
        }

        /// <summary>
        /// Gets or Sets the cached latch value, does not access HW.  A set leaves the register
        /// marked dirty. Use Apply() to write dirty values to HW or use Write() to "directly"
        /// write value to HW.
        /// 
        /// Most implementations preserve all bits outside the current bitfield definition,
        /// but some implementations (CommandField32, CommandField64) clear all other bits.
        /// </summary>
        public virtual long Value64
        {
            get
            {
                return (uint)ReadBitField();
            }
            set
            {
                WriteBitField( (int)value );
            }
        }

        /// <summary>
        /// Gets the bit field size in bits.
        /// </summary>
        public int Size
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the bit field starting bit position.
        /// </summary>
        public int StartBit
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the bit field ending bit position.
        /// </summary>
        public int EndBit
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the mask for the bit field within the register.
        /// </summary>
        public long Mask
        {
            get;
            private set;
        }

        /// <summary>
        /// Writes the value to HW.
        /// 
        /// Most implementations preserve all bits outside the current bitfield definition,
        /// but some implementations (CommandField32, CommandField64) clear all other bits.
        /// </summary>
        /// <param name="value">value to write.</param>
        public virtual void Write( int value )
        {
            Write( null, value );
        }

        /// <summary>
        /// Writes the value to HW.
        /// 
        /// Most implementations preserve all bits outside the current bitfield definition,
        /// but some implementations (CommandField32, CommandField64) clear all other bits.
        /// </summary>
        /// <param name="driver">driver(port in moonstone terms) to write to.</param>
        /// <param name="value">value to write.</param>
        public virtual void Write( IRegDriver driver, int value )
        {
            // NOTE: use WriteBitField to get the appropriate register logging
            WriteBitField( value );
            if( mWriteDelegate != null )
            {
                mWriteDelegate( driver, mBitFieldValue );
            }
        }

        /// <summary>
        /// Reads BitField value from HW.
        /// </summary>
        public virtual int Read()
        {
            if( mReadDelegate != null )
            {
                mBitFieldValue = mReadDelegate();
            }
            // NOTE: use ReadBitField to get the appropriate register logging
            return ReadBitField();
        }

        /// <summary>
        /// Writes the value to HW.
        /// 
        /// Most implementations preserve all bits outside the current bitfield definition,
        /// but some implementations (CommandField32, CommandField64) clear all other bits.
        /// </summary>
        /// <param name="value">value to write.</param>
        public void Write( long value )
        {
            Write( (int)value );
        }

        /// <summary>
        /// Reads BitField value from HW.
        /// </summary>
        public virtual long Read64()
        {
            return (uint)Read();
        }

        /// <summary>
        /// Call Apply() of the associated register.  This conditionally writes the register's
        /// value (if dirty) to hardware.
        /// </summary>
        /// <param name="forceApply"></param>
        public virtual void Apply( bool forceApply )
        {
            Apply( null, forceApply );
        }

        /// <summary>
        /// Call Apply() of the associated register.  This conditionally writes the register's
        /// value (if dirty) to hardware using the specified driver.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="forceApply"></param>
        public virtual void Apply( IRegDriver driver, bool forceApply )
        {
            if( mWriteDelegate != null )
            {
                mWriteDelegate( driver, mBitFieldValue );
            }
        }

        /// <summary>
        /// Return a reference to the register this field belongs to.  THIS MAY BE NULL!!!
        /// </summary>
        public IRegister Register
        {
            get
            {
                // No register available!
                return null;
            }
        }

        #region Register bit locking methods

        public virtual void LockBits()
        {
            throw new NotImplementedException();
        }

        public virtual void UnlockBits()
        {
            throw new NotImplementedException();
        }

        #endregion Register bit locking methods

        #endregion

        #region implementation

        protected int ReadBitField()
        {
            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                mLogger.LogAppend(
                    new RegisterLoggingEvent( LogLevel.Fine, mBitFieldValue, this )
                        {
                            Operation = RegisterLoggingEvent.OperationType.BFGetVal
                        } );
            }
            return mBitFieldValue;
        }

        protected void WriteBitField( int value )
        {
            if( mLogger.IsLoggingEnabledFor( LogLevel.Fine ) )
            {
                mLogger.LogAppend(
                    new RegisterLoggingEvent( LogLevel.Fine, value, this )
                        {
                            Operation = RegisterLoggingEvent.OperationType.BFSetVal
                        } );
            }
            mBitFieldValue = value;
        }

        #endregion implementation
    }
}
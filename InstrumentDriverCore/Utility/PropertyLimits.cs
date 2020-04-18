/******************************************************************************
 *                                                                         
 *                .
 *               All rights reserved.
 *
 *****************************************************************************/
using System;

namespace InstrumentDriver.Core.Utility
{
    /// <summary>
    /// A simple class for holding a fixed range for validation.
    /// </summary>
    public class PropertyLimits < T > where T : IComparable
    {
        #region variables

        private readonly bool mHasMinimum;
        private readonly bool mHasMaximum;
        private readonly string mCustomErrorFormat;

        #endregion variables

        #region constructors

        /// <summary>
        /// isMinimum==true:  Minimum limit (no maximum)
        /// isMinimum==false: Maximum limit (no minimum)
        /// 
        /// Sets Units to Units.Unitless
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="isMinimum">if true limit specifies a minimum, if false limit specifies a maximum</param>
        public PropertyLimits( T limit, bool isMinimum )
            : this( limit, isMinimum, Units.Unitless, string.Empty )
        {
        }

        /// <summary>
        /// isMinimum==true:  Minimum limit (no maximum)
        /// isMinimum==false: Maximum limit (no minimum)
        /// 
        /// Sets Units to Units.Unitless
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="isMinimum">if true limit specifies a minimum, if false limit specifies a maximum</param>
        /// <param name="customErrorFormat">custom error message: string.Format(customErrorFormat, field, value, min, max, units)</param>
        public PropertyLimits( T limit, bool isMinimum, string customErrorFormat )
            : this( limit, isMinimum, Units.Unitless, customErrorFormat )
        {
        }

        /// <summary>
        /// isMinimum==true:  Minimum limit (no maximum)
        /// isMinimum==false: Maximum limit (no minimum)
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="isMinimum">if true limit specifies a minimum, if false limit specifies a maximum</param>
        /// <param name="units"></param>
        public PropertyLimits( T limit, bool isMinimum, Units units )
            : this( limit, isMinimum, units, string.Empty )
        {
        }

        /// <summary>
        /// isMinimum==true:  Minimum limit (no maximum)
        /// isMinimum==false: Maximum limit (no minimum)
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="isMinimum">if true limit specifies a minimum, if false limit specifies a maximum</param>
        /// <param name="units"></param>
        /// <param name="customErrorFormat">custom error message: string.Format(customErrorFormat, field, value, min, max, units)</param>
        public PropertyLimits( T limit, bool isMinimum, Units units, string customErrorFormat )
        {
            Units = units;
            mCustomErrorFormat = customErrorFormat;
            if( isMinimum )
            {
                Minimum = limit;
                mHasMinimum = true;
            }
            else
            {
                Maximum = limit;
                mHasMaximum = true;
            }
        }

        /// <summary>
        /// Sets Units to Units.Unitless
        /// </summary>
        /// <param name="minimum"></param>
        /// <param name="maximum"></param>
        public PropertyLimits( T minimum, T maximum )
            : this( minimum, maximum, Units.Unitless, string.Empty )
        {
        }

        /// <summary>
        /// Sets Units to Units.Unitless
        /// </summary>
        /// <param name="minimum"></param>
        /// <param name="maximum"></param>
        /// <param name="customErrorFormat">custom error message: string.Format(customErrorFormat, field, value, min, max, units)</param>
        public PropertyLimits( T minimum, T maximum, string customErrorFormat )
            : this( minimum, maximum, Units.Unitless, customErrorFormat )
        {
        }

        /// <summary>
        /// Uses default error message.
        /// </summary>
        /// <param name="minimum"></param>
        /// <param name="maximum"></param>
        /// <param name="units"></param>
        public PropertyLimits( T minimum, T maximum, Units units )
            : this( minimum, maximum, units, string.Empty )
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="minimum"></param>
        /// <param name="maximum"></param>
        /// <param name="units"></param>
        /// <param name="customErrorFormat">custom error message: string.Format(customErrorFormat, field, value, min, max, units)</param>
        public PropertyLimits( T minimum, T maximum, Units units, string customErrorFormat )
        {
            if( minimum.CompareTo( maximum ) > 0 )
            {
                throw new InternalApplicationException(
                    string.Format( "The lower bounds ({0}) must be less than or equal to the upper bounds ({1})",
                                   minimum,
                                   maximum ) );
            }
            Units = units;
            mCustomErrorFormat = customErrorFormat;
            Maximum = maximum;
            Minimum = minimum;
            mHasMinimum = true;
            mHasMaximum = true;
        }

        #endregion constructors

        #region properties

        public T Maximum
        {
            get;
            private set;
        }

        public T Minimum
        {
            get;
            private set;
        }

        public Units Units
        {
            get;
            private set;
        }

        #endregion properties

        #region methods

        /// <summary>
        /// USE OF THIS METHOD IS STRONGLY DISCOURAGED
        /// 
        /// Clip a value against the upper and lower limits and return the clipped value.
        /// 
        /// Silently coercing a value is usually not a good idea as the client may not know
        /// the value was clipped and draw the wrong conclusion about measurements.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public T Clip( T value )
        {
            if( mHasMinimum && value.CompareTo( Minimum ) < 0 )
            {
                return Minimum;
            }
            if( mHasMaximum && value.CompareTo( Maximum ) > 0 )
            {
                return Maximum;
            }
            return value;
        }

        /// <summary>
        /// Is the parameter in the bounded range?
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool IsValid( T value )
        {
            return ( !mHasMinimum || value.CompareTo( Minimum ) >= 0 ) &&
                   ( !mHasMaximum || value.CompareTo( Maximum ) <= 0 );
        }

        /// <summary>
        /// A convenience method to check the validity of 'value' and, if not valid,
        /// generate an error message and report an error.
        ///
        /// If you don't want the default error (InvalidParameterException) use:
        /// <code>
        ///     if( ! mExampleValueLimits.IsValid( value ) {
        ///         ErrorLog.ThrowException( new MyException( ... ) );
        ///     } 
        /// </code>
        /// </summary>
        /// <param name="value">the value to check</param>
        /// <param name="fieldName">user visible name of field/parameter</param>
        /// <param name="errorLog">the ErrorLog used to report errors ... may be empty</param>
        public void CheckValidity( T value, string fieldName, IErrorLog errorLog )
        {
            if( !IsValid( value ) )
            {
                // Not valid ... generate error message and report error...
                Exception ex = new InvalidParameterException( RangeValidationErrorMessage( value, fieldName ) );
                if( errorLog == null )
                {
                    throw ex;
                }
                errorLog.ThrowException( ex );
            }
        }

        /// <summary>
        /// Produce a standard error message of one of the forms
        /// 
        ///    The FIELDNAME value VALUE is not in the valid range [MIN,MAX] UNITS.
        ///    The FIELDNAME value VALUE is less than the minimum MIN UNITS.
        ///    The FIELDNAME value VALUE is greater than the maximum MAX UNITS.
        ///    CUSTOM
        /// 
        /// 'CUSTOM' is a client supplied format (specified to the constructor of PropertyLimits)
        /// that is exercised via: 
        /// 
        ///    string.Format( mCustomErrorFormat, fieldName, value, Minimum, Maximum, Units )
        /// 
        /// </summary>
        /// <param name="value">The value used in IsValid</param>
        /// <param name="fieldName">The FIELDNAME that this is used against, see the example above</param>
        /// <returns></returns>
        public string RangeValidationErrorMessage( T value, string fieldName )
        {
            // NOTE: code copied from Apogee ... not sure why isn't simply using engineering notation???
            // TODO: replace the switch/multiple formats with engineering notation formatting
            if( !string.IsNullOrEmpty( mCustomErrorFormat ) )
            {
                return string.Format( mCustomErrorFormat,
                                      fieldName,
                                      value,
                                      Minimum,
                                      Maximum,
                                      Units );
            }
            if( !mHasMaximum )
            {
                switch( Units )
                {
                    case Units.Microseconds:
                    case Units.Milliseconds:
                    case Units.Nanoseconds:
                        return string.Format(
                            "The {0} value {1:0.0##} is less than the minimum {2:0.0##} {3}.",
                            fieldName,
                            value,
                            Minimum,
                            Units );

                    case Units.Seconds:
                        return string.Format( "The {0} value {1:E2} is less than the minimum {2:E2} {3}.",
                                              fieldName,
                                              value,
                                              Minimum,
                                              Units );

                    case Units.GHz:
                    case Units.Hz:
                    case Units.Hz_per_volt:
                    case Units.KHz:
                    case Units.MHz:
                        return string.Format( "The {0} value {1:E2} is less than the minimum {2:E2} {3}.",
                                              fieldName,
                                              value,
                                              Minimum,
                                              Units );


                    case Units.Amps:
                    case Units.dBm:
                    case Units.dBm_per_volt:
                    case Units.dB:
                    case Units.mA:
                    case Units.mV:
                    case Units.mW:
                    case Units.Volts:
                    case Units.W:
                    case Units.vRMS:
                    case Units.Degrees:
                        return
                            string.Format( "The {0} value {1:0.0#} is less than the minimum {2:0.0#} {3}.",
                                           fieldName,
                                           value,
                                           Minimum,
                                           Units );

                    case Units.Unitless:
                        return string.Format( "The {0} value {1} is less than the minimum {2}",
                                              fieldName,
                                              value,
                                              Minimum );

                    default:
                        return string.Format( "The {0} value {1} is less than the minimum {2} {3}.",
                                              fieldName,
                                              value,
                                              Minimum,
                                              Units );
                }
            }
            if( !mHasMinimum )
            {
                switch( Units )
                {
                    case Units.Microseconds:
                    case Units.Milliseconds:
                    case Units.Nanoseconds:
                        return string.Format(
                            "The {0} value {1:0.0##} is greater than the maximum {2:0.0##} {3}.",
                            fieldName,
                            value,
                            Maximum,
                            Units );

                    case Units.Seconds:
                        return string.Format( "The {0} value {1:E2} is greater than the maximum {2:E2} {3}.",
                                              fieldName,
                                              value,
                                              Maximum,
                                              Units );

                    case Units.GHz:
                    case Units.Hz:
                    case Units.Hz_per_volt:
                    case Units.KHz:
                    case Units.MHz:
                        return string.Format( "The {0} value {1:E2} is greater than the maximum {2:E2} {3}.",
                                              fieldName,
                                              value,
                                              Maximum,
                                              Units );


                    case Units.Amps:
                    case Units.dBm:
                    case Units.dBm_per_volt:
                    case Units.dB:
                    case Units.mA:
                    case Units.mV:
                    case Units.mW:
                    case Units.Volts:
                    case Units.W:
                    case Units.vRMS:
                    case Units.Degrees:
                        return
                            string.Format( "The {0} value {1:0.0#} is greater than the maximum {2:0.0#} {3}.",
                                           fieldName,
                                           value,
                                           Maximum,
                                           Units );

                    case Units.Unitless:
                        return string.Format( "The {0} value {1} is greater than the maximum {2}",
                                              fieldName,
                                              value,
                                              Maximum );

                    default:
                        return string.Format( "The {0} value {1} is greater than the maximum {2} {3}.",
                                              fieldName,
                                              value,
                                              Maximum,
                                              Units );
                }
            }
            // else  mHasMinimum && mHasMaximum
            {
                switch( Units )
                {
                    case Units.Microseconds:
                    case Units.Milliseconds:
                    case Units.Nanoseconds:
                        return string.Format(
                            "The {0} value {1:0.0##} is not in the valid range [{2:0.0##},{3:0.0##}] {4}.",
                            fieldName,
                            value,
                            Minimum,
                            Maximum,
                            Units );

                    case Units.Seconds:
                        return string.Format( "The {0} value {1:E2} is not in the valid range [{2:E2},{3:0.0}] {4}.",
                                              fieldName,
                                              value,
                                              Minimum,
                                              Maximum,
                                              Units );

                    case Units.GHz:
                    case Units.Hz:
                    case Units.Hz_per_volt:
                    case Units.KHz:
                    case Units.MHz:
                        return string.Format( "The {0} value {1:E2} is not in the valid range [{2:E2},{3:E2}] {4}.",
                                              fieldName,
                                              value,
                                              Minimum,
                                              Maximum,
                                              Units );


                    case Units.Amps:
                    case Units.dBm:
                    case Units.dBm_per_volt:
                    case Units.dB:
                    case Units.mA:
                    case Units.mV:
                    case Units.mW:
                    case Units.Volts:
                    case Units.W:
                    case Units.vRMS:
                    case Units.Degrees:
                        return
                            string.Format( "The {0} value {1:0.0#} is not in the valid range [{2:0.0#},{3:0.0#}] {4}.",
                                           fieldName,
                                           value,
                                           Minimum,
                                           Maximum,
                                           Units );

                    case Units.Unitless:
                        return string.Format( "The {0} value {1} is not in the valid range [{2},{3}]",
                                              fieldName,
                                              value,
                                              Minimum,
                                              Maximum );

                    default:
                        return string.Format( "The {0} value {1} is not in the valid range [{2},{3}] {4}.",
                                              fieldName,
                                              value,
                                              Minimum,
                                              Maximum,
                                              Units );
                }
            }
        }

        #endregion methods
    }
}
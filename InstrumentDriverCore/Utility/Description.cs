/******************************************************************************
 *                                                                         
 *               Copyright 2012-2013 .
 *               All rights reserved.
 *
 *****************************************************************************/

using System;
using System.Collections;
using System.Reflection;

namespace InstrumentDriver.Core.Utility
{
    /// <summary>
    /// This is a handy enum attribute that can be used to define a custom attribute for text on the enum member
    /// </summary>
    public sealed class Description : Attribute
    {
        public Description( string value )
        {
            Text = value;
        }

        public string Text
        {
            private set;
            get;
        }

        /// <summary>
        /// Reflects into an enum to determine if it has a Description, if it does, return it, otherwise use ToString on the enum.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static string GetAttribute( Enum e )
        {
            // Use reflection to pull off the Description field...
            FieldInfo fieldInfo = e.GetType().GetField( e.ToString() );
            Description[] enumAttributes =
                (Description[])fieldInfo.GetCustomAttributes( typeof( Description ), false );
            // If the Description attribute was there, get the text, if not, push the enum.ToString() back.
            return enumAttributes.Length > 0 ? enumAttributes[ 0 ].Text : e.ToString();
        }

        /// <summary>
        /// Converts the <see cref="Enum"/> type to an <see cref="System.Collections.IList"/> compatible object.
        /// </summary>
        /// <param name="type">The <see cref="Enum"/> type.</param>
        /// <returns>An <see cref="System.Collections.IList"/> containing the enumerated type value and description.</returns>
        public static IList ToList( Type type )
        {
            if( type == null )
            {
                throw new ArgumentNullException( "type" );
            }

            ArrayList list = new ArrayList();
            Array enumValues = Enum.GetValues( type );

            foreach( Enum value in enumValues )
            {
                list.Add( new DictionaryEntry( value, GetAttribute( value ) ) );
            }

            return list;
        }

        /// <summary>
        /// Find the matching enumeration by the description...  Useful if you want to persist the description
        /// for better readability but need to recast the description into the enum.
        /// 
        /// Here's how to use it
        /// string description = blah-blah;
        /// cyclicPrefix = (eCyclicPrefix)Description.FindByDescription( description, typeof( eCyclicPrefix ) );
        /// 
        /// </summary>
        /// <param name="description">The enum description attribute to lookup</param>
        /// <param name="type">The typeof( enum ) to lookup against.</param>
        /// <returns>The enum value or null if not found.</returns>
        public static Enum FindByDescription( string description, Type type )
        {
            IList enums = ToList( type );
            foreach( DictionaryEntry entry in enums )
            {
                if( description.Equals( entry.Value ) )
                {
                    return (Enum)entry.Key;
                }
            }

            return null;
        }
    }
}
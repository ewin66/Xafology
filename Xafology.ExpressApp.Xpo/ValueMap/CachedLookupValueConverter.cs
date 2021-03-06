﻿using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xafology.ExpressApp.Xpo.ValueMap
{

    public class CachedLookupValueConverter : Xafology.ExpressApp.Xpo.ValueMap.ILookupValueConverter
    {

        private readonly Xafology.ExpressApp.Xpo.ValueMap.CachedXPCollections cacheDictionary;

        public LogUnmatchedLookupsDelegate UnmatchedLookupLogger { get; set; }

        public CachedLookupValueConverter(CachedXPCollections cacheDictionary)
        {
            if (cacheDictionary == null)
                throw new ArgumentNullException("cacheDictionary");
            this.cacheDictionary = cacheDictionary;
        }

        /// <summary>
        /// Get XPO object from memory
        /// </summary>
        /// <param name="value">Original value</param>
        /// <param name="memberInfo">XPO member</param>
        /// <param name="session"></param>
        /// <returns></returns>
        public IXPObject ConvertToXpObject(string value, IMemberInfo memberInfo, Session session,
            bool createMember = false)
        {
            var lookupDefaultMember = memberInfo.MemberTypeInfo.DefaultMember;

            var cachedObjects = cacheDictionary[memberInfo.MemberType];
            object newValue = null;

            if (!TryGetCachedLookupObject(cachedObjects, lookupDefaultMember, value, out newValue))
            {
                if (createMember)
                {
                    newValue = CreateMember(session, memberInfo.MemberType, lookupDefaultMember.Name, value);
                    cachedObjects.Add(newValue);
                }

                LogXpObjectsNotFound(memberInfo.MemberType, value);
            }

            return newValue as IXPObject;
        }

        private bool TryGetCachedLookupObject(XPCollection cachedObjects, 
            IMemberInfo lookupMemberInfo, 
            string value,  out object newValue)
        {
            newValue = null;

            // assign lookup object to return value
            foreach (var obj in cachedObjects)
            {
                string tmpValue = Convert.ToString(lookupMemberInfo.GetValue(obj));
                if (tmpValue.Trim().ToLower() == value.Trim().ToLower()) // case-insensitive 
                {
                    newValue = obj;
                    break;
                }
            }

            if (newValue == null)
                return false;
            return true;
        }

        /// <summary>
        /// Create lookup object if it does not exist
        /// </summary>
        /// <param name="session">Session for creating the missing object</param>
        /// <param name="memberType">Type of the lookup object. You can get this using MemberInfo.MemberType</param>
        /// <param name="propertyName">property name of the lookup object</param>
        /// <param name="propertyValue">property value of the lookup object</param>
        /// <returns></returns>
        private IXPObject CreateMember(Session session, Type memberType, string propertyName, string propertyValue)
        {
            var newObj = (IXPObject)Activator.CreateInstance(memberType, session);
            ReflectionHelper.SetMemberValue(newObj, propertyName, propertyValue);
            //newObj.Session.Save(newObj);
            return newObj;
        }

        private void LogXpObjectsNotFound(Type memberType, string value)
        {
            if (UnmatchedLookupLogger != null)
                UnmatchedLookupLogger(memberType, value);
        }


    }
}

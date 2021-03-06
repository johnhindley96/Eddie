﻿// <eddie_source_header>
// This file is part of Eddie/AirVPN software.
// Copyright (C)2014-2018 AirVPN (support@airvpn.org) / https://airvpn.org
//
// Eddie is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Eddie is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Eddie. If not, see <http://www.gnu.org/licenses/>.
// </eddie_source_header>
//
// 20 June 2018 - author: promind - initial release. (a tribute to the 1859 Perugia uprising occurred on 20 June 1859 and in memory of those brave inhabitants who fought for the liberty of Perugia)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Android.App;
using Android.Content;
using Android.Database;
using Android.OS;
using Android.Provider;
using Java.IO;
using Java.Util;

namespace Eddie.NativeAndroidApp
{
    public class SupportTools
    {
        private Context appContext = null;
        private Handler dialogHandler = null;

        public SupportTools(Context context)
        {
            appContext = context;
        }

        public Dictionary<string, string> GetOpenVPNProfile(Android.Net.Uri profileUri)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            System.IO.Stream inputStream = null;
            StreamReader reader = null;
            string line = "", profile = "";
            string[] item = null;
            int MAX_FILE_SIZE = 200000;
            int validItems = 0;
            long fileSize = 0;

            ICursor uriCursor = appContext.ContentResolver.Query(profileUri, null, null, null, null);

            try
            {
                if(uriCursor != null)
                {
                    if(uriCursor.MoveToFirst())
                        fileSize = uriCursor.GetLong(uriCursor.GetColumnIndex(OpenableColumns.Size));
                    else
                        fileSize = 0;
                }
                else
                    fileSize = 0;
            }
            catch
            {
                fileSize = 0;
            }
            finally
            {
                if(uriCursor != null)
                    uriCursor.Close();
            }

            if(fileSize == 0 || fileSize > MAX_FILE_SIZE)
            {
                result.Add("status", "invalid");
                
                return result;
            }

            try
            {
                inputStream = appContext.ContentResolver.OpenInputStream(profileUri);
            }
            catch(Java.IO.FileNotFoundException)
            {
                result.Add("status", "not_found");

                return result;
            }
            catch(Java.Lang.SecurityException)
            {
                result.Add("status", "no_permission");

                return result;
            }

            try
            {
                reader = new StreamReader(inputStream);
    
                validItems = 0;
    
                while((line = reader.ReadLine()) != null)
                {
                    profile += line + "\n";
    
                    item = line.Split(' ');
    
                    if(item[0] == "remote")
                    {
                        if(result.ContainsKey("server") == false)
                            result.Add("server", item[1]);

                        if(result.ContainsKey("port") == false)
                            result.Add("port", item[2]);
    
                        validItems++;
                    }
                    else if(item[0] == "proto")
                    {
                        if(result.ContainsKey("protocol") == false)
                            result.Add("protocol", item[1]);
    
                        validItems++;
                    }
                }
                
                reader.Close();
            }
            catch(Java.IO.IOException)
            {
                validItems = 0;
            }

            if(validItems == 2)
            {
				//result.Add("name", profileUri.Path.Substring(profileUri.Path.LastIndexOf('/') + 1));
				string name = GetRealPathFromURI(appContext, profileUri);
				result.Add("name", name.Substring(name.LastIndexOf('/') + 1));

				result.Add("profile", profile);
                result.Add("status", "ok");
            }
            else
            {
                result.Add("status", "invalid");
            }

            return result;
        }
		
		public void InfoDialog(int resource)
        {
            InfoDialog(appContext.Resources.GetString(resource));
        }

        public void InfoDialog(string message)
        {
            AlertDialog.Builder dialogBuilder = new AlertDialog.Builder(appContext);

            AlertDialog infoDialog = dialogBuilder.Create();

            infoDialog.SetTitle(appContext.Resources.GetString(Resource.String.eddie));

            infoDialog.SetIcon(global::Android.Resource.Drawable.IcDialogInfo);

            infoDialog.SetMessage(message);

            infoDialog.SetButton(appContext.Resources.GetString(Resource.String.ok), (c, ev) =>
            {
                infoDialog.Dismiss();
            });

            infoDialog.Show();
        }
        public bool ConfirmationDialog(int resource)
        {
            return ConfirmationDialog(appContext.Resources.GetString(resource));
        }

        public bool ConfirmationDialog(string message)
        {
            bool confirm = false;

            dialogHandler = new Handler(m => { throw new Java.Lang.RuntimeException(); });

            AlertDialog.Builder dialogBuilder = new AlertDialog.Builder(appContext);

            AlertDialog confirmationDialog = dialogBuilder.Create();

            confirmationDialog.SetTitle(appContext.Resources.GetString(Resource.String.eddie));

            confirmationDialog.SetIcon(global::Android.Resource.Drawable.IcDialogAlert);

            confirmationDialog.SetMessage(message);

            confirmationDialog.SetButton(appContext.Resources.GetString(Resource.String.yes), (c, ev) =>
            {
                confirm = true;

                dialogHandler.SendMessage(dialogHandler.ObtainMessage());

                confirmationDialog.Dismiss();
            });

            confirmationDialog.SetButton2(appContext.Resources.GetString(Resource.String.no), (c, ev) =>
            {
                confirm = false;

                dialogHandler.SendMessage(dialogHandler.ObtainMessage());

                confirmationDialog.Dismiss();
            });

            confirmationDialog.Show();

            try
            {
                Looper.Loop();
            }
            catch(Java.Lang.RuntimeException)
            {
            }
            
            return confirm;
        }

        public static bool Empty(string value)
        {
            return string.IsNullOrEmpty(value);
        }

        public static bool BoolCast(int value)
        {
            return value != 0 ? true : false;
        }

        public static void SafeDispose(IDisposable disposable) 
        { 
            try 
            { 
                if(disposable != null) 
                    disposable.Dispose(); 
            } 
            catch 
            { 
            
            } 
        } 

        public static void SafeClose(ICloseable closable)
        {
            try
            {
                if(closable != null)
                    closable.Close();
            }
            catch
            {
    
            }
        }

        public static void Sleep(int milliseconds)
        {
            System.Threading.Thread.Sleep(milliseconds);
        }

        public static String GetExceptionDetails(Exception e)
        {
            String details = e.Message;

            StackTrace stack = new StackTrace(e);
            
            if(stack.FrameCount > 0)
            {
                StackFrame frame = stack.GetFrame(stack.FrameCount - 1);
                
                if(frame != null)
                {
                    MethodBase method = frame.GetMethod();
                    
                    if(method != null)
                    {
                        string methodName = method.Name;
                        
                        if(Empty(methodName) == false)
                        {
                            string className = method.ReflectedType != null ? method.ReflectedType.FullName : null;
                            
                            if(Empty(className) == false)
                                methodName = className + "." + methodName;

                            if(Empty(details) == false)
                                details += " ";

                            string fileName = frame.GetFileName();
                            int fileLine = frame.GetFileLineNumber();
                            int fileColumn = frame.GetFileColumnNumber();

                            string fileInfo = "";
                            
                            if((Empty(fileName) == false) || (fileLine != 0) || (fileColumn != 0))
                                fileInfo = " at file:line:column " + String.Format("{0}:{1}:{2}", Empty(fileName) ? "?" : fileName, fileLine != 0 ? Convert.ToString(fileLine) : "?", fileColumn != 0 ? Convert.ToString(fileColumn) : "?");

                            details += "(" + methodName + fileInfo + ")";
                        }
                    }
                }
            }

            return details;
        }

		/* Fabry */
		/**
 * Get a file path from a Uri. This will get the the path for Storage Access
 * Framework Documents, as well as the _data field for the MediaStore and
 * other file-based ContentProviders.
 *
 * @param context The context.
 * @param uri The Uri to query.
 * @author paulburke
 */
		public static string GetRealPathFromURI(Context context, Android.Net.Uri uri)
		{

			bool isKitKat = Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Kitkat;

			// DocumentProvider
			if (isKitKat && Android.Provider.DocumentsContract.IsDocumentUri(context, uri))
			{
				// ExternalStorageProvider
				if (isExternalStorageDocument(uri))
				{
					string docId = Android.Provider.DocumentsContract.GetDocumentId(uri);
					string[] split = docId.Split(':');
					string type = split[0];

					if ("primary".Equals(type, StringComparison.OrdinalIgnoreCase))
					{
						return Android.OS.Environment.ExternalStorageDirectory + "/" + split[1];
					}

					// TODO handle non-primary volumes
				}
				// DownloadsProvider
				else if (isDownloadsDocument(uri))
				{

					string id = Android.Provider.DocumentsContract.GetDocumentId(uri);
					Android.Net.Uri contentUri = ContentUris.WithAppendedId(Android.Net.Uri.Parse("content://downloads/public_downloads"), Convert.ToInt64(id));

					return getDataColumn(context, contentUri, null, null);
				}
				// MediaProvider
				else if (isMediaDocument(uri))
				{
					string docId = Android.Provider.DocumentsContract.GetDocumentId(uri);
					string[] split = docId.Split(':');
					string type = split[0];

					Android.Net.Uri contentUri = null;
					if ("image".Equals(type))
					{
						contentUri = Android.Provider.MediaStore.Images.Media.ExternalContentUri;
					}
					else if ("video".Equals(type))
					{
						contentUri = Android.Provider.MediaStore.Video.Media.ExternalContentUri;
					}
					else if ("audio".Equals(type))
					{
						contentUri = Android.Provider.MediaStore.Audio.Media.ExternalContentUri;
					}

					string selection = "_id=?";
					string[] selectionArgs = new string[] {
					split[1]
			};

					return getDataColumn(context, contentUri, selection, selectionArgs);
				}
			}
			// MediaStore (and general)
			else if ("content".Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase))
			{
				return getDataColumn(context, uri, null, null);
			}
			// File
			else if ("file".Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase))
			{
				return uri.Path;
			}

			return null;
		}

		/**
         * Get the value of the data column for this Uri. This is useful for
         * MediaStore Uris, and other file-based ContentProviders.
         *
         * @param context The context.
         * @param uri The Uri to query.
         * @param selection (Optional) Filter used in the query.
         * @param selectionArgs (Optional) Selection arguments used in the query.
         * @return The value of the _data column, which is typically a file path.
         */
		public static String getDataColumn(Context context, Android.Net.Uri uri, String selection,
				String[] selectionArgs)
		{

			Android.Database.ICursor cursor = null;
			string column = "_data";
			string[] projection = {
				column
			};

			try
			{
				cursor = context.ContentResolver.Query(uri, projection, selection, selectionArgs,
						null);
				if (cursor != null && cursor.MoveToFirst())
				{
					int column_index = cursor.GetColumnIndexOrThrow(column);
					return cursor.GetString(column_index);
				}
			}
			finally
			{
				if (cursor != null)
					cursor.Close();
			}
			return null;
		}


		/**
         * @param uri The Uri to check.
         * @return Whether the Uri authority is ExternalStorageProvider.
         */
		public static bool isExternalStorageDocument(Android.Net.Uri uri)
		{
			return "com.android.externalstorage.documents".Equals(uri.Authority);
		}

		/**
         * @param uri The Uri to check.
         * @return Whether the Uri authority is DownloadsProvider.
         */
		public static bool isDownloadsDocument(Android.Net.Uri uri)
		{
			return "com.android.providers.downloads.documents".Equals(uri.Authority);
		}

		/**
         * @param uri The Uri to check.
         * @return Whether the Uri authority is MediaProvider.
         */
		public static bool isMediaDocument(Android.Net.Uri uri)
		{
			return "com.android.providers.media.documents".Equals(uri.Authority);
		}
	}
}

// Copyright Epic Games, Inc. All Rights Reserved.
// This file is automatically generated. Changes to this file may be overwritten.

namespace Epic.OnlineServices.Sessions
{
	/// <summary>
	/// Input parameters for the <see cref="SessionDetails.CopySessionAttributeByIndex" /> function.
	/// </summary>
	public class SessionDetailsCopySessionAttributeByIndexOptions
	{
		/// <summary>
		/// The index of the attribute to retrieve
		/// <seealso cref="SessionDetails.GetSessionAttributeCount" />
		/// </summary>
		public uint AttrIndex { get; set; }
	}

	[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 8)]
	internal struct SessionDetailsCopySessionAttributeByIndexOptionsInternal : ISettable, System.IDisposable
	{
		private int m_ApiVersion;
		private uint m_AttrIndex;

		public uint AttrIndex
		{
			set
			{
				m_AttrIndex = value;
			}
		}

		public void Set(SessionDetailsCopySessionAttributeByIndexOptions other)
		{
			if (other != null)
			{
				m_ApiVersion = SessionDetails.SessiondetailsCopysessionattributebyindexApiLatest;
				AttrIndex = other.AttrIndex;
			}
		}

		public void Set(object other)
		{
			Set(other as SessionDetailsCopySessionAttributeByIndexOptions);
		}

		public void Dispose()
		{
		}
	}
}
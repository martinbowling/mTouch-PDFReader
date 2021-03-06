//
// mTouch-PDFReader library
//   IDocumentNoteManager.cs
//
//  Author:
//       Alexander Matsibarov (macasun) <amatsibarov@gmail.com>
//
//  Copyright (c) 2012 Alexander Matsibarov
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using mTouchPDFReader.Library.Data.Objects;

namespace mTouchPDFReader.Library.Interfaces
{
	public interface IDocumentNoteManager
	{
		/// <summary>
		/// Creates the new <see cref="DocumentNote"/> object.
		/// </summary>.
		/// </param>
		DocumentNote GetNew(int docId, string note);

		/// <summary>
		/// Gets the <see cref="DocumentNote"/> object by the <see cref="docId"/>.
		/// </summary>
		/// <param name="docId">The PDF document Id.</param>
		/// <returns>The <see cref="DocumentNote"/> object.</returns>
		DocumentNote Load(int docId);
		
		/// <summary>
		/// Saves the see cref="T"/> object.
		/// </summary>
		/// <param name="note">The <see cref="DocumentNote"/> object.</param>
		void Save(DocumentNote note);
	}
}


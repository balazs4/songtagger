//
//  Release.cs
//
//  Author:
//       balazs4 <balazs4web@gmail.com>
//
//  Copyright (c) 2013 
//
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//

using System;
using System.Collections.Generic;

namespace SongTagger.Core
{
    public interface IRelease : IEntity
    {
        IAlbum Album { get; }
        
        IEnumerable<ISong> Songs { get; }
    }
    

    internal class Release : IRelease
    {
        public Release()
        {
            QuerySongDelegate = (IRelease release) => new List<ISong>();
        }

        #region IRelease implementation

        public IAlbum Album { get; internal set; }

        public IEnumerable<ISong> Songs 
        { 
            get 
            {
                return songList ?? (songList = QuerySongDelegate(this));
            }
        }

        #endregion

        #region IEntity implementation

        public Guid Id{ get; internal set; }

        public string Name{ get; internal set; }

        #endregion
        private IEnumerable<ISong> songList;

        internal delegate IEnumerable<ISong> QuerySongDelegateAction(IRelease release);

        public QuerySongDelegateAction QuerySongDelegate
        {
            get;
            internal set;
        }
    }
    
}
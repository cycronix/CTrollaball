/*
Copyright 2018 Cycronix
 
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
 
    http://www.apache.org/licenses/LICENSE-2.0
 
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CTworldNS
{
    public class CTworld
    {
        public string player { get; set; }
        public double time { get; set; }
//        public string mode { get; set; }
        //        public List<CTobject> objects;
        public Dictionary<String, CTobject> objects;
    }

	public class CTobject
	{
		public string id { get; set; }
		public string model { get; set; }
//		public Boolean state { get; set; }
		public Vector3 pos { get; set; }
		public Quaternion rot { get; set; }
		public Vector3 scale { get; set; }
//		public string link { get; set; }
		public Color color { get; set; }
//		public List<Vector3> points { get; set; }
		public string custom { set; get; }
//		public Boolean isWorld { set; get; }
    }
}
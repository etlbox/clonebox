﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CloneBox {
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field |AttributeTargets.Class)]
    public sealed class DoNotClone : Attribute {
      
        public string Name { get; set; }

        public DoNotClone() {
        }
    }
}

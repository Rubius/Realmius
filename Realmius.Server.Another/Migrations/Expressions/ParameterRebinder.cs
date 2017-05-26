////////////////////////////////////////////////////////////////////////////
//
// Copyright 2017 Rubius
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Realmius.Server.Expressions
{
    public class ParameterRebinder : ExpressionVisitor
    {
        private readonly Dictionary<ParameterExpression, ParameterExpression> _map;

        public ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map)
        {
            this._map = map ?? new Dictionary<ParameterExpression, ParameterExpression>();
        }

        public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> map, Expression exp)
        {
            return new ParameterRebinder(map).Visit(exp);
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            ParameterExpression replacement;

            if (_map.TryGetValue(p, out replacement))
            {
                p = replacement;
            }

            return base.VisitParameter(p);
        }
    }
}
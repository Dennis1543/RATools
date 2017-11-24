﻿using Jamiras.DataModels;
using RATools.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RATools.ViewModels
{
    [DebuggerDisplay("{Title}")]
    public class AchievementComparisonViewModel : AchievementViewModel
    {
        public AchievementComparisonViewModel(GameViewModel owner, Achievement compareAchievement)
            : base(owner)
        {
            _compareAchievement = compareAchievement;
        }

        public Achievement Other { get { return _compareAchievement; } }
        private readonly Achievement _compareAchievement;

        protected override List<RequirementGroupViewModel> BuildRequirementGroups()
        {
            if (_compareAchievement == null)
                return base.BuildRequirementGroups();

            var groups = new List<RequirementGroupViewModel>();
            groups.Add(new RequirementGroupViewModel("Core", Achievement.CoreRequirements, _compareAchievement.CoreRequirements, _owner.Notes));

            int i = 0;
            var altEnumerator = Achievement.AlternateRequirements.GetEnumerator();
            var altCompareEnumerator = _compareAchievement.AlternateRequirements.GetEnumerator();

            while (altEnumerator.MoveNext())
            {
                i++;

                IEnumerable<Requirement> altCompareRequirements = altCompareEnumerator.MoveNext() ? altCompareEnumerator.Current : new Requirement[0];
                groups.Add(new RequirementGroupViewModel("Alt " + i, altEnumerator.Current, altCompareRequirements, _owner.Notes));
            }

            while (altCompareEnumerator.MoveNext())
            {
                i++;
                groups.Add(new RequirementGroupViewModel("Alt " + i, new Requirement[0], altCompareEnumerator.Current, _owner.Notes));
            }

            return groups;
        }

        protected override ModifiedState GetModified()
        {
            if (_compareAchievement == null)
                return base.GetModified();

            bool isModified = false;

            if (IsTitleModified = (Achievement.Title != _compareAchievement.Title))
                isModified = true;

            if (IsDescriptionModified = (Achievement.Description != _compareAchievement.Description))
                isModified = true;

            if (IsPointsModified = (Achievement.Points != _compareAchievement.Points))
                isModified = true;

            foreach (var group in RequirementGroups)
            {
                foreach (var requirement in group.Requirements.OfType<RequirementComparisonViewModel>())
                {
                    if (requirement.IsModified)
                    {
                        isModified = true;
                        break;
                    }
                }
            }

            return isModified ? ModifiedState.Modified : ModifiedState.Unmodified;
        }

        public static readonly ModelProperty IsTitleModifiedProperty = ModelProperty.Register(typeof(AchievementComparisonViewModel), "IsTitleModified", typeof(bool), false);
        public bool IsTitleModified
        {
            get { return (bool)GetValue(IsTitleModifiedProperty); }
            private set { SetValue(IsTitleModifiedProperty, value); }
        }

        public static readonly ModelProperty IsDescriptionModifiedProperty = ModelProperty.Register(typeof(AchievementComparisonViewModel), "IsDescriptionModified", typeof(bool), false);
        public bool IsDescriptionModified
        {
            get { return (bool)GetValue(IsDescriptionModifiedProperty); }
            private set { SetValue(IsDescriptionModifiedProperty, value); }
        }

        public static readonly ModelProperty IsPointsModifiedProperty = ModelProperty.Register(typeof(AchievementComparisonViewModel), "IsPointsModified", typeof(bool), false);
        public bool IsPointsModified
        {
            get { return (bool)GetValue(IsPointsModifiedProperty); }
            private set { SetValue(IsPointsModifiedProperty, value); }
        }

        //public CommandBase UpdateLocalCommand { get; private set; }
        //private void ExecuteUpdateLocal()
        //{
        //    UpdateLocal();

        //    foreach (var field in ModifiableTextFields)
        //        field.LocalText = field.Text;

        //    if (_status != null)
        //    {
        //        _status = null;
        //        OnPropertyChanged(() => Status);
        //    }
        //}
        //protected virtual void UpdateLocal()
        //{
        //    if (Id > 0)
        //        Achievement.Id = Id;
        //    if (!String.IsNullOrEmpty(BadgeName))
        //        Achievement.BadgeName = BadgeName;
        //    _owner.UpdateLocal(Achievement, Title.LocalText);
        //}
    }
}

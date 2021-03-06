﻿using IronCow;
using IronCow.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Milkman.Common;
using Milkman.Controls;
using System;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Windows.ApplicationModel.Store;

namespace Milkman
{
    public partial class TaskListByLocationPage : PhoneApplicationPage
    {
        public static bool sReload = true;
        public static bool sFirstLaunch = false;

        #region Task List Property

        public static readonly DependencyProperty AllTasksProperty =
               DependencyProperty.Register("AllTasks", typeof(ObservableCollection<Task>), typeof(TaskListByLocationPage), new PropertyMetadata(new ObservableCollection<Task>()));

        public ObservableCollection<Task> AllTasks
        {
            get { return (ObservableCollection<Task>)GetValue(AllTasksProperty); }
            set { SetValue(AllTasksProperty, value); }
        }

        public static readonly DependencyProperty TodayTasksProperty =
               DependencyProperty.Register("TodayTasks", typeof(ObservableCollection<Task>), typeof(TaskListByLocationPage), new PropertyMetadata(new ObservableCollection<Task>()));

        public ObservableCollection<Task> TodayTasks
        {
            get { return (ObservableCollection<Task>)GetValue(TodayTasksProperty); }
            set { SetValue(TodayTasksProperty, value); }
        }

        public static readonly DependencyProperty TomorrowTasksProperty =
               DependencyProperty.Register("TomorrowTasks", typeof(ObservableCollection<Task>), typeof(TaskListByLocationPage), new PropertyMetadata(new ObservableCollection<Task>()));

        public ObservableCollection<Task> TomorrowTasks
        {
            get { return (ObservableCollection<Task>)GetValue(TomorrowTasksProperty); }
            set { SetValue(TomorrowTasksProperty, value); }
        }

        public static readonly DependencyProperty OverdueTasksProperty =
               DependencyProperty.Register("OverdueTasks", typeof(ObservableCollection<Task>), typeof(TaskListByLocationPage), new PropertyMetadata(new ObservableCollection<Task>()));

        public ObservableCollection<Task> OverdueTasks
        {
            get { return (ObservableCollection<Task>)GetValue(OverdueTasksProperty); }
            set { SetValue(OverdueTasksProperty, value); }
        }

        public static readonly DependencyProperty WeekTasksProperty =
               DependencyProperty.Register("WeekTasks", typeof(ObservableCollection<Task>), typeof(TaskListByLocationPage), new PropertyMetadata(new ObservableCollection<Task>()));

        public ObservableCollection<Task> WeekTasks
        {
            get { return (ObservableCollection<Task>)GetValue(WeekTasksProperty); }
            set { SetValue(WeekTasksProperty, value); }
        }

        #endregion

        #region Construction and Navigation

        AddTaskDialog dlgAddTask;

        ApplicationBarIconButton dashboard;
        ApplicationBarIconButton add;
        ApplicationBarIconButton select;
        ApplicationBarIconButton sync;
        ApplicationBarIconButton complete;
        ApplicationBarIconButton postpone;
        ApplicationBarIconButton delete;

        ApplicationBarMenuItem settings;
        ApplicationBarMenuItem about;
        ApplicationBarMenuItem feedback;
        ApplicationBarMenuItem donate;
        ApplicationBarMenuItem signOut;

        GeoCoordinateWatcher _watcher;

        public TaskListByLocationPage()
        {
            InitializeComponent();

            App.UnhandledExceptionHandled += new EventHandler<ApplicationUnhandledExceptionEventArgs>(App_UnhandledExceptionHandled);

            this.BuildApplicationBar();

            AppSettings settings = new AppSettings();

            if (settings.LocationRemindersEnabled == true)
            {
                _watcher = new GeoCoordinateWatcher();
                _watcher.Start();
            }
        }

        private void BuildApplicationBar()
        {
            dashboard = new ApplicationBarIconButton();
            dashboard.IconUri = new Uri("/Resources/dashboard.png", UriKind.RelativeOrAbsolute);
            dashboard.Text = Strings.DashboardMenuLower;
            dashboard.Click += btnDashboard_Click;

            add = new ApplicationBarIconButton();
            add.IconUri = new Uri("/Resources/add.png", UriKind.RelativeOrAbsolute);
            add.Text = Strings.AddMenuLower;
            add.Click += btnAdd_Click;

            select = new ApplicationBarIconButton();
            select.IconUri = new Uri("/Resources/select.png", UriKind.RelativeOrAbsolute);
            select.Text = Strings.SelectMenuLower;
            select.Click += btnSelect_Click;

            sync = new ApplicationBarIconButton();
            sync.IconUri = new Uri("/Resources/retry.png", UriKind.RelativeOrAbsolute);
            sync.Text = Strings.SyncMenuLower;
            sync.Click += btnSync_Click;

            complete = new ApplicationBarIconButton();
            complete.IconUri = new Uri("/Resources/complete.png", UriKind.RelativeOrAbsolute);
            complete.Text = Strings.CompleteMenuLower;
            complete.Click += btnComplete_Click;

            postpone = new ApplicationBarIconButton();
            postpone.IconUri = new Uri("/Resources/postpone.png", UriKind.RelativeOrAbsolute);
            postpone.Text = Strings.PostponeMenuLower;
            postpone.Click += btnPostpone_Click;

            delete = new ApplicationBarIconButton();
            delete.IconUri = new Uri("/Resources/delete.png", UriKind.RelativeOrAbsolute);
            delete.Text = Strings.DeleteMenuLower;
            delete.Click += btnDelete_Click;

            settings = new ApplicationBarMenuItem();
            settings.Text = Strings.SettingsMenuLower;
            settings.Click += mnuSettings_Click;

            about = new ApplicationBarMenuItem();
            about.Text = Strings.AboutMenuLower;
            about.Click += mnuAbout_Click;

            feedback = new ApplicationBarMenuItem();
            feedback.Text = Strings.FeedbackMenuLower;
            feedback.Click += mnuFeedback_Click;

            donate = new ApplicationBarMenuItem();
            donate.Text = Strings.DonateMenuLower;
            donate.Click += mnuDonate_Click;

            signOut = new ApplicationBarMenuItem();
            signOut.Text = Strings.SignOutMenuLower;
            signOut.Click += mnuSignOut_Click;

            // build application bar
            ApplicationBar.Buttons.Add(dashboard);
            ApplicationBar.Buttons.Add(add);
            ApplicationBar.Buttons.Add(select);
            ApplicationBar.Buttons.Add(sync);

            ApplicationBar.MenuItems.Add(settings);
            ApplicationBar.MenuItems.Add(about);
            ApplicationBar.MenuItems.Add(feedback);
            ApplicationBar.MenuItems.Add(donate);
            ApplicationBar.MenuItems.Add(signOut);

            // disable buttons when working offline
            if (App.RtmClient.Syncing == false)
            {
                add.IsEnabled = false;
                sync.IsEnabled = false;
                complete.IsEnabled = false;
                postpone.IsEnabled = false;
                delete.IsEnabled = false;
            }
        }

        private void App_UnhandledExceptionHandled(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                GlobalLoading.Instance.IsLoading = false;
            });
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            GlobalLoading.Instance.IsLoadingText(Strings.Loading);

            if (e.IsNavigationInitiator == false)
            {
                LittleWatson.CheckForPreviousException(true);

                sFirstLaunch = true;
            }

            LoadData();

            if (e.IsNavigationInitiator == false)
            {
                App.CheckTimezone();

                FeedbackHelper.PromptForRating();
            }

            base.OnNavigatedTo(e);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (this.lstAll.IsSelectionEnabled)
            {
                this.lstAll.IsSelectionEnabled = false;
                e.Cancel = true;
            }

            base.OnBackKeyPress(e);
        }

        #endregion

        #region Loading Data

        private void SyncData()
        {
            if (sReload)
            {
                if (!string.IsNullOrEmpty(App.RtmClient.AuthToken))
                {
                    SmartDispatcher.BeginInvoke(() =>
                    {
                        GlobalLoading.Instance.IsLoadingText(Strings.SyncingTasks);
                    });

                    App.RtmClient.SyncEverything(() =>
                    {
                        LoadData();
                    });
                }
                else
                {
                    Login();
                }
            }
            else
            {
                SmartDispatcher.BeginInvoke(() =>
                {
                    GlobalLoading.Instance.IsLoading = false;
                });
            }

            sReload = false;
        }

        private void LoadData()
        {
            LoadDataInBackground();

            Deployment.Current.Dispatcher.BeginInvoke(delegate
            {
                if (_watcher != null)
                    NotificationsManager.SetupNotifications(_watcher.Position.Location);
                else
                    NotificationsManager.SetupNotifications(null);
            });
        }

        private void LoadDataInBackground()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                GlobalLoading.Instance.IsLoadingText(Strings.SyncingTasks);

                string id;
                if (NavigationContext.QueryString.TryGetValue("id", out id))
                {
                    var tempAllTasks = new SortableObservableCollection<Task>();
                    var tempTodayTasks = new SortableObservableCollection<Task>();
                    var tempTomorrowTasks = new SortableObservableCollection<Task>();
                    var tempOverdueTasks = new SortableObservableCollection<Task>();
                    var tempWeekTasks = new SortableObservableCollection<Task>();

                    AppSettings settings = new AppSettings();

                    double radius;
                    if (settings.NearbyRadius == 0)
                        radius = 1.0;
                    else if (settings.NearbyRadius == 1)
                        radius = 2.0;
                    else if (settings.NearbyRadius == 2)
                        radius = 5.0;
                    else if (settings.NearbyRadius == 3)
                        radius = 10.0;
                    else if (settings.NearbyRadius == 3)
                        radius = 20.0;
                    else
                        radius = 0.0;

                    foreach (Task t in App.RtmClient.GetNearbyTasks(_watcher.Position.Location.Latitude, _watcher.Position.Location.Longitude, radius))
                    {
                        if (t.IsCompleted == true ||
                            t.IsDeleted == true) continue;

                        tempAllTasks.Add(t);

                        if (t.DueDateTime.HasValue &&
                            t.DueDateTime.Value.Date == DateTime.Now.Date)
                        {
                            tempTodayTasks.Add(t);
                        }

                        if (t.DueDateTime.HasValue &&
                            t.DueDateTime.Value.Date == DateTime.Now.Date.AddDays(1))
                        {
                            tempTomorrowTasks.Add(t);
                        }

                        if (t.IsLate == true)
                        {
                            tempOverdueTasks.Add(t);
                        }

                        if (t.DueDateTime.HasValue &&
                            t.DueDateTime.Value.Date <= DateTime.Now.Date.AddDays(7))
                        {
                            tempWeekTasks.Add(t);
                        }
                    }

                    if (settings.IgnorePriorityEnabled == true)
                    {
                        tempAllTasks.Sort(Task.CompareByDate);
                        tempTodayTasks.Sort(Task.CompareByDate);
                        tempTomorrowTasks.Sort(Task.CompareByDate);
                        tempOverdueTasks.Sort(Task.CompareByDate);
                        tempWeekTasks.Sort(Task.CompareByDate);
                    }
                    else
                    {
                        tempAllTasks.Sort(Task.CompareByPriority);
                        tempTodayTasks.Sort(Task.CompareByPriority);
                        tempTomorrowTasks.Sort(Task.CompareByPriority);
                        tempOverdueTasks.Sort(Task.CompareByPriority);
                        tempWeekTasks.Sort(Task.CompareByPriority);
                    }

                    AllTasks = tempAllTasks;
                    TodayTasks = tempTodayTasks;
                    TomorrowTasks = tempTomorrowTasks;
                    OverdueTasks = tempOverdueTasks;
                    WeekTasks = tempWeekTasks;

                    ToggleLoadingText();
                    ToggleEmptyText();

                    GlobalLoading.Instance.IsLoading = false;

                    ShowLastUpdatedStatus();
                }
            });
        }

        private void ToggleLoadingText()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                this.txtAllLoading.Visibility = System.Windows.Visibility.Collapsed;
                this.txtTodayLoading.Visibility = System.Windows.Visibility.Collapsed;
                this.txtTomorrowLoading.Visibility = System.Windows.Visibility.Collapsed;
                this.txtOverdueLoading.Visibility = System.Windows.Visibility.Collapsed;
                this.txtWeekLoading.Visibility = System.Windows.Visibility.Collapsed;
            });
        }

        private void ToggleEmptyText()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                if (AllTasks.Count == 0)
                    this.txtAllEmpty.Visibility = System.Windows.Visibility.Visible;
                else
                    this.txtAllEmpty.Visibility = System.Windows.Visibility.Collapsed;

                if (TodayTasks.Count == 0)
                    this.txtTodayEmpty.Visibility = System.Windows.Visibility.Visible;
                else
                    this.txtTodayEmpty.Visibility = System.Windows.Visibility.Collapsed;

                if (TomorrowTasks.Count == 0)
                    this.txtTomorrowEmpty.Visibility = System.Windows.Visibility.Visible;
                else
                    this.txtTomorrowEmpty.Visibility = System.Windows.Visibility.Collapsed;

                if (OverdueTasks.Count == 0)
                    this.txtOverdueEmpty.Visibility = System.Windows.Visibility.Visible;
                else
                    this.txtOverdueEmpty.Visibility = System.Windows.Visibility.Collapsed;

                if (WeekTasks.Count == 0)
                    this.txtWeekEmpty.Visibility = System.Windows.Visibility.Visible;
                else
                    this.txtWeekEmpty.Visibility = System.Windows.Visibility.Collapsed;
            });
        }

        private void ShowLastUpdatedStatus()
        {
            if (sFirstLaunch == true)
            {
                int minutes = Convert.ToInt32((DateTime.Now - App.LastUpdated).TotalMinutes);

                if (minutes < 2)
                    GlobalLoading.Instance.StatusText(Strings.UpToDate);
                else if (minutes > 60)
                    GlobalLoading.Instance.StatusText(Strings.LastUpdated + " " + Strings.OverAnHourAgo);
                else
                    GlobalLoading.Instance.StatusText(Strings.LastUpdated + " " + minutes + " " + Strings.MinutesAgo);

                System.ComponentModel.BackgroundWorker b = new System.ComponentModel.BackgroundWorker();
                b.DoWork += (s, e) =>
                {
                    System.Threading.Thread.Sleep(4000);

                    SmartDispatcher.BeginInvoke(() =>
                    {
                        GlobalLoading.Instance.ClearStatusText();
                    });
                };

                sFirstLaunch = false;

                b.RunWorkerAsync();
            }
        }

        public void Login()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                NavigationService.Navigate(new Uri("/WelcomePage.xaml", UriKind.Relative));
            });
        }

        #endregion

        #region Event Handlers

        private void btnDashboard_Click(object sender, EventArgs e)
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                if (NavigationService.CanGoBack)
                    NavigationService.GoBack();
                else
                    NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            });
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            AppSettings settings = new AppSettings();

            if (settings.AddTaskDialogEnabled == true)
            {
                this.dlgAddTask = new AddTaskDialog();

                CustomMessageBox messageBox = this.dlgAddTask.CreateDialog("");

                messageBox.Dismissed += (s1, e1) =>
                {
                    switch (e1.Result)
                    {
                        case CustomMessageBoxResult.LeftButton:
                            AddTask(this.dlgAddTask.txtDetails.Text);

                            break;
                        default:
                            break;
                    }
                };

                messageBox.Show();
            }
            else
            {
                NavigationService.Navigate(new Uri("/AddTaskPage.xaml", UriKind.Relative));
            }
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            LongListMultiSelector target = null;
            if (this.pivLayout.SelectedIndex == 0)
                target = this.lstAll;
            else if (this.pivLayout.SelectedIndex == 1)
                target = this.lstToday;
            else if (this.pivLayout.SelectedIndex == 2)
                target = this.lstTomorrow;
            else if (this.pivLayout.SelectedIndex == 3)
                target = this.lstOverdue;
            else if (this.pivLayout.SelectedIndex == 4)
                target = this.lstWeek;

            target.IsSelectionEnabled = true;
        }

        private void btnComplete_Click(object sender, EventArgs e)
        {
            if (GlobalLoading.Instance.IsLoading) return;

            LongListMultiSelector target = null;
            if (this.pivLayout.SelectedIndex == 0)
                target = this.lstAll;
            else if (this.pivLayout.SelectedIndex == 1)
                target = this.lstToday;
            else if (this.pivLayout.SelectedIndex == 2)
                target = this.lstTomorrow;
            else if (this.pivLayout.SelectedIndex == 3)
                target = this.lstOverdue;
            else if (this.pivLayout.SelectedIndex == 4)
                target = this.lstWeek;

            string messageBoxText;
            if (target.SelectedItems.Count == 1)
                messageBoxText = Strings.CompleteTaskSingleDialog;
            else
                messageBoxText = Strings.CompleteTaskPluralDialog;

            CustomMessageBox messageBox = new CustomMessageBox()
            {
                Caption = Strings.CompleteDialogTitle,
                Message = messageBoxText,
                LeftButtonContent = Strings.YesLower,
                RightButtonContent = Strings.NoLower,
                IsFullScreen = false
            };

            messageBox.Dismissed += (s1, e1) =>
            {
                switch (e1.Result)
                {
                    case CustomMessageBoxResult.LeftButton:
                        bool isMultiple = target.SelectedItems.Count > 1;

                        while (target.SelectedItems.Count > 0)
                        {
                            CompleteTask((Task)target.SelectedItems[0], isMultiple);
                            target.SelectedItems.RemoveAt(0);
                        }

                        target.IsSelectionEnabled = false;

                        break;
                    default:
                        break;
                }
            };

            messageBox.Show();
        }

        private void btnPostpone_Click(object sender, EventArgs e)
        {
            if (GlobalLoading.Instance.IsLoading) return;

            LongListMultiSelector target = null;
            if (this.pivLayout.SelectedIndex == 0)
                target = this.lstAll;
            else if (this.pivLayout.SelectedIndex == 1)
                target = this.lstToday;
            else if (this.pivLayout.SelectedIndex == 2)
                target = this.lstTomorrow;
            else if (this.pivLayout.SelectedIndex == 3)
                target = this.lstOverdue;
            else if (this.pivLayout.SelectedIndex == 4)
                target = this.lstWeek;

            string messageBoxText;
            if (target.SelectedItems.Count == 1)
                messageBoxText = Strings.PostponeTaskSingleDialog;
            else
                messageBoxText = Strings.PostponeTaskPluralDialog;

            CustomMessageBox messageBox = new CustomMessageBox()
            {
                Caption = Strings.PostponeDialogTitle,
                Message = messageBoxText,
                LeftButtonContent = Strings.YesLower,
                RightButtonContent = Strings.NoLower,
                IsFullScreen = false
            };

            messageBox.Dismissed += (s1, e1) =>
            {
                switch (e1.Result)
                {
                    case CustomMessageBoxResult.LeftButton:
                        bool isMultiple = target.SelectedItems.Count > 1;

                        while (target.SelectedItems.Count > 0)
                        {
                            PostponeTask((Task)target.SelectedItems[0], isMultiple);
                            target.SelectedItems.RemoveAt(0);
                        }

                        target.IsSelectionEnabled = false;

                        break;
                    default:
                        break;
                }
            };

            messageBox.Show();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (GlobalLoading.Instance.IsLoading) return;

            LongListMultiSelector target = null;
            if (this.pivLayout.SelectedIndex == 0)
                target = this.lstAll;
            else if (this.pivLayout.SelectedIndex == 1)
                target = this.lstToday;
            else if (this.pivLayout.SelectedIndex == 2)
                target = this.lstTomorrow;
            else if (this.pivLayout.SelectedIndex == 3)
                target = this.lstOverdue;
            else if (this.pivLayout.SelectedIndex == 4)
                target = this.lstWeek;

            string messageBoxText;
            if (target.SelectedItems.Count == 1)
                messageBoxText = Strings.DeleteTaskSingleDialog;
            else
                messageBoxText = Strings.DeleteTaskPluralDialog;

            CustomMessageBox messageBox = new CustomMessageBox()
            {
                Caption = Strings.DeleteTaskDialogTitle,
                Message = messageBoxText,
                LeftButtonContent = Strings.YesLower,
                RightButtonContent = Strings.NoLower,
                IsFullScreen = false
            };

            messageBox.Dismissed += (s1, e1) =>
            {
                switch (e1.Result)
                {
                    case CustomMessageBoxResult.LeftButton:
                        bool isMultiple = target.SelectedItems.Count > 1;

                        while (target.SelectedItems.Count > 0)
                        {
                            DeleteTask((Task)target.SelectedItems[0], isMultiple);
                            target.SelectedItems.RemoveAt(0);
                        }

                        target.IsSelectionEnabled = false;

                        break;
                    default:
                        break;
                }
            };

            messageBox.Show();
        }

        private void btnSync_Click(object sender, EventArgs e)
        {
            sReload = true;
            sFirstLaunch = true;

            SyncData();
        }

        private void LongListMultiSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LongListMultiSelector target = (LongListMultiSelector)sender;
            ApplicationBarIconButton i = (ApplicationBarIconButton)ApplicationBar.Buttons[0]; // complete
            ApplicationBarIconButton j = (ApplicationBarIconButton)ApplicationBar.Buttons[1]; // postpone
            ApplicationBarIconButton k = (ApplicationBarIconButton)ApplicationBar.Buttons[2]; // delete

            if (target.IsSelectionEnabled)
            {
                if (target.SelectedItems.Count > 0)
                {
                    i.IsEnabled = true;
                    j.IsEnabled = true;
                    k.IsEnabled = true;
                }
                else
                {
                    i.IsEnabled = false;
                    j.IsEnabled = false;
                    k.IsEnabled = false;
                }
            }
            else
            {
                i.IsEnabled = true;
                j.IsEnabled = true;
                k.IsEnabled = true;
            }

            // disable buttons when working offline
            if (App.RtmClient.Syncing == false)
            {
                add.IsEnabled = false;
                sync.IsEnabled = false;
                complete.IsEnabled = false;
                postpone.IsEnabled = false;
                delete.IsEnabled = false;
            }
        }

        private void LongListMultiSelector_IsSelectionEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            LongListMultiSelector target = (LongListMultiSelector)sender;

            while (ApplicationBar.Buttons.Count > 0)
            {
                ApplicationBar.Buttons.RemoveAt(0);
            }

            while (ApplicationBar.MenuItems.Count > 0)
            {
                ApplicationBar.MenuItems.RemoveAt(0);
            }

            if ((bool)e.NewValue)
            {
                this.pivLayout.IsLocked = true;

                ApplicationBar.Buttons.Add(complete);
                ApplicationBarIconButton i = (ApplicationBarIconButton)ApplicationBar.Buttons[0];
                i.IsEnabled = false;

                ApplicationBar.Buttons.Add(postpone);
                ApplicationBarIconButton j = (ApplicationBarIconButton)ApplicationBar.Buttons[1];
                j.IsEnabled = false;

                ApplicationBar.Buttons.Add(delete);
                ApplicationBarIconButton k = (ApplicationBarIconButton)ApplicationBar.Buttons[2];
                k.IsEnabled = false;
            }
            else
            {
                this.pivLayout.IsLocked = false;

                ApplicationBar.Buttons.Add(dashboard);
                ApplicationBar.Buttons.Add(add);
                ApplicationBar.Buttons.Add(select);
                ApplicationBar.Buttons.Add(sync);

                ApplicationBar.MenuItems.Add(settings);
                ApplicationBar.MenuItems.Add(about);
                ApplicationBar.MenuItems.Add(feedback);
                ApplicationBar.MenuItems.Add(donate);
                ApplicationBar.MenuItems.Add(signOut);
            }

            Thickness margin = target.Margin;

            if (target.IsSelectionEnabled)
                margin.Left = margin.Left - 12;
            else
                margin.Left = margin.Left + 12;

            target.Margin = margin;

            // disable buttons when working offline
            if (App.RtmClient.Syncing == false)
            {
                add.IsEnabled = false;
                sync.IsEnabled = false;
                complete.IsEnabled = false;
                postpone.IsEnabled = false;
                delete.IsEnabled = false;
            }
        }

        private void ItemContent_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (GlobalLoading.Instance.IsLoading) return;

            Task item = ((FrameworkElement)sender).DataContext as Task;

            if (item != null)
                NavigationService.Navigate(new Uri("/TaskDetailsPage.xaml?id=" + item.Id, UriKind.Relative));
        }

        private void mnuSettings_Click(object sender, EventArgs e)
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
            });
        }

        private void mnuAbout_Click(object sender, EventArgs e)
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                NavigationService.Navigate(new Uri("/YourLastAboutDialog;component/AboutPage.xaml", UriKind.Relative));
            });
        }

        private void mnuFeedback_Click(object sender, EventArgs e)
        {
            FeedbackHelper.Default.Feedback();
        }

        private async void mnuDonate_Click(object sender, EventArgs e)
        {
            try
            {
                var productList = await CurrentApp.LoadListingInformationAsync();
                var product = productList.ProductListings.FirstOrDefault(p => p.Value.ProductType == ProductType.Consumable);
                var receipt = await CurrentApp.RequestProductPurchaseAsync(product.Value.ProductId, true);

                if (CurrentApp.LicenseInformation.ProductLicenses[product.Value.ProductId].IsActive)
                {
                    CurrentApp.ReportProductFulfillment(product.Value.ProductId);

                    MessageBox.Show(Strings.DonateDialog, Strings.DonateDialogTitle, MessageBoxButton.OK);
                }
            }
            catch (Exception ex)
            {
                // do nothing
            }
        }

        private void mnuSignOut_Click(object sender, EventArgs e)
        {
            CustomMessageBox messageBox = new CustomMessageBox()
            {
                Caption = Strings.SignOutDialogTitle,
                Message = Strings.SignOutDialog,
                LeftButtonContent = Strings.YesLower,
                RightButtonContent = Strings.NoLower,
                IsFullScreen = false
            };

            messageBox.Dismissed += (s1, e1) =>
            {
                switch (e1.Result)
                {
                    case CustomMessageBoxResult.LeftButton:
                        App.DeleteData();
                        Login();

                        break;
                    default:
                        break;
                }
            };

            messageBox.Show();
        }

        private Task MostRecentTaskClick
        {
            get;
            set;
        }

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement)
            {
                FrameworkElement frameworkElement = (FrameworkElement)e.OriginalSource;
                if (frameworkElement.DataContext is Task)
                {
                    MostRecentTaskClick = (Task)frameworkElement.DataContext;
                }
            }
            base.OnMouseLeftButtonDown(e);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem target = (MenuItem)sender;
            ContextMenu parent = (ContextMenu)target.Parent;

            if (target.Header.ToString() == Strings.CompleteMenuLower)
            {
                CustomMessageBox messageBox = new CustomMessageBox()
                {
                    Caption = Strings.CompleteDialogTitle,
                    Message = Strings.CompleteDialog,
                    LeftButtonContent = Strings.YesLower,
                    RightButtonContent = Strings.NoLower,
                    IsFullScreen = false
                };

                messageBox.Dismissed += (s1, e1) =>
                {
                    switch (e1.Result)
                    {
                        case CustomMessageBoxResult.LeftButton:
                            CompleteTask(MostRecentTaskClick, false);

                            break;
                        default:
                            break;
                    }
                };

                messageBox.Show();
            }
            else if (target.Header.ToString() == Strings.PostponeMenuLower)
            {
                CustomMessageBox messageBox = new CustomMessageBox()
                {
                    Caption = Strings.PostponeDialogTitle,
                    Message = Strings.PostponeDialog,
                    LeftButtonContent = Strings.YesLower,
                    RightButtonContent = Strings.NoLower,
                    IsFullScreen = false
                };

                messageBox.Dismissed += (s1, e1) =>
                {
                    switch (e1.Result)
                    {
                        case CustomMessageBoxResult.LeftButton:
                            PostponeTask(MostRecentTaskClick, false);

                            break;
                        default:
                            break;
                    }
                };

                messageBox.Show();
            }
            else if (target.Header.ToString() == Strings.DeleteMenuLower)
            {
                CustomMessageBox messageBox = new CustomMessageBox()
                {
                    Caption = Strings.DeleteTaskDialogTitle,
                    Message = Strings.DeleteTaskDialog,
                    LeftButtonContent = Strings.YesLower,
                    RightButtonContent = Strings.NoLower,
                    IsFullScreen = false
                };

                messageBox.Dismissed += (s1, e1) =>
                {
                    switch (e1.Result)
                    {
                        case CustomMessageBoxResult.LeftButton:
                            DeleteTask(MostRecentTaskClick, false);

                            break;
                        default:
                            break;
                    }
                };

                messageBox.Show();
            }
        }

        #endregion

        #region Task Methods

        private void AddTask(string smartAddText)
        {
            GlobalLoading.Instance.IsLoadingText(Strings.AddingTask);

            string input = smartAddText;
            if (input.Contains('#') == false)
            {
                TaskList defaultList = App.RtmClient.GetDefaultTaskList();
                if (defaultList.IsSmart == false)
                    input = input + " #" + defaultList.Name;
            }

            App.RtmClient.AddTask(input, true, null, () =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    GlobalLoading.Instance.IsLoading = false;
                });

                sReload = true;
                LoadData();
            });
        }

        private void CompleteTask(Task data, bool isMultiple)
        {
            if (isMultiple == true)
                GlobalLoading.Instance.IsLoadingText(Strings.CompletingTasks);
            else
                GlobalLoading.Instance.IsLoadingText(Strings.CompletingTask);

            data.Complete(() =>
            {
                App.RtmClient.CacheTasks(() =>
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        GlobalLoading.Instance.IsLoading = false;
                    });

                    sReload = true;
                    LoadData();
                });
            });
        }

        private void PostponeTask(Task data, bool isMultiple)
        {
            if (isMultiple == true)
                GlobalLoading.Instance.IsLoadingText(Strings.PostponingTasks);
            else
                GlobalLoading.Instance.IsLoadingText(Strings.PostponingTask);

            data.Postpone(() =>
            {
                App.RtmClient.CacheTasks(() =>
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        GlobalLoading.Instance.IsLoading = false;
                    });

                    sReload = true;
                    LoadData();
                });
            });
        }

        private void DeleteTask(Task data, bool isMultiple)
        {
            if (isMultiple == true)
                GlobalLoading.Instance.IsLoadingText(Strings.DeletingTasks);
            else
                GlobalLoading.Instance.IsLoadingText(Strings.DeletingTask);

            data.Delete(() =>
            {
                App.RtmClient.CacheTasks(() =>
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        GlobalLoading.Instance.IsLoading = false;
                    });

                    sReload = true;
                    LoadData();
                });
            });
        }

        #endregion
    }
}
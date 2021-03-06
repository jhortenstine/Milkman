﻿using IronCow;
using IronCow.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Milkman.Common;
using System;
using System.Linq;
using System.Windows;
using Windows.ApplicationModel.Store;

namespace Milkman
{
    public partial class AuthorizationPage : PhoneApplicationPage
    {
        #region Construction and Navigation

        private string Frob { get; set; }

        ApplicationBarIconButton complete;
        ApplicationBarIconButton retry;

        ApplicationBarMenuItem about;
        ApplicationBarMenuItem feedback;
        ApplicationBarMenuItem donate;

        public AuthorizationPage()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(AuthorizationPage_Loaded);
            App.UnhandledExceptionHandled += new EventHandler<ApplicationUnhandledExceptionEventArgs>(App_UnhandledExceptionHandled);

            this.BuildApplicationBar();
        }

        private void BuildApplicationBar()
        {
            complete = new ApplicationBarIconButton();
            complete.IconUri = new Uri("/Resources/complete.png", UriKind.RelativeOrAbsolute);
            complete.Text = Strings.CompleteMenuLower;
            complete.Click += btnComplete_Click;
            complete.IsEnabled = false;

            retry = new ApplicationBarIconButton();
            retry.IconUri = new Uri("/Resources/retry.png", UriKind.RelativeOrAbsolute);
            retry.Text = Strings.RetryMenuLower;
            retry.Click += btnRetry_Click;

            about = new ApplicationBarMenuItem();
            about.Text = Strings.AboutMenuLower;
            about.Click += mnuAbout_Click;

            feedback = new ApplicationBarMenuItem();
            feedback.Text = Strings.FeedbackMenuLower;
            feedback.Click += mnuFeedback_Click;

            donate = new ApplicationBarMenuItem();
            donate.Text = Strings.DonateMenuLower;
            donate.Click += mnuDonate_Click;

            // build application bar
            ApplicationBar.Buttons.Add(complete);
            ApplicationBar.Buttons.Add(retry);

            ApplicationBar.MenuItems.Add(about);
            ApplicationBar.MenuItems.Add(feedback);
            ApplicationBar.MenuItems.Add(donate);
        }

        private void AuthorizationPage_Loaded(object sender, RoutedEventArgs e)
        {
            StartAuth();
            MessageBox.Show(Strings.AuthorizationDialog.Replace("\\n", "\n"), Strings.AuthorizationDialogTitle, MessageBoxButton.OK);
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
            base.OnNavigatedTo(e);
        }

        private void StartAuth()
        {
            GlobalLoading.Instance.IsLoadingText(Strings.Loading);

            App.RtmClient.GetFrob((string frob) =>
            {
                Frob = frob;
                string url = App.RtmClient.GetAuthenticationUrl(frob, AuthenticationPermissions.Delete);

                Dispatcher.BeginInvoke(() =>
                {
                    GlobalLoading.Instance.IsLoading = false;
                    webAuthorization.Navigate(new Uri(url));
                });
            });
        }

        #endregion

        #region Event Handling

        private void btnComplete_Click(object sender, EventArgs e)
        {
            // only do something if Frob is present
            if (!string.IsNullOrEmpty(Frob))
            {
                GlobalLoading.Instance.IsLoadingText(Strings.Authorizing);

                App.RtmClient.GetToken(Frob, (string token, User user) =>
                {
                    // create timeline
                    App.RtmClient.GetOrStartTimeline((int timeline) =>
                    {
                        App.SaveData();
                        
                        Dispatcher.BeginInvoke(() =>
                        {
                            GlobalLoading.Instance.IsLoading = false;

                            if (NavigationService.CanGoBack)
                            {
                                MainPage.sReload = true;
                                
                                NavigationService.RemoveBackEntry();
                                NavigationService.Navigate(new Uri("/MainPage.xaml?IsFirstRun=true", UriKind.Relative));
                            }
                        });
                    });
                });
            }
        }

        private void btnRetry_Click(object sender, EventArgs e)
        {
            StartAuth();
        }

        private void webAuthorization_Navigating(object sender, NavigatingEventArgs e)
        {
            GlobalLoading.Instance.IsLoadingText(Strings.Loading);
        }

        private void webAuthorization_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            GlobalLoading.Instance.IsLoading = false;

            if (this.webAuthorization.SaveToString().Contains("<title>Remember The Milk - Application successfully authorized</title>"))
            {
                complete.IsEnabled = true;
            }
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

        #endregion
    }
}
<UserControl x:Class="Maktab.App.Wpf.Views.DashboardView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             mc:Ignorable="d"
             d:DesignHeight="600" d:DesignWidth="900"
             FlowDirection="RightToLeft">
    <Grid Margin="12">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <!-- Header -->
        <Border Grid.Row="0" Background="#0F766E" Padding="14" CornerRadius="6" Margin="0,0,0,12">
            <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                <TextBlock Text="🏠 داشبورد" FontSize="20" FontWeight="Bold" Foreground="White" />
                <TextBlock Text=" (Dashboard)" FontSize="13" Foreground="#99F6E4" VerticalAlignment="Center" Margin="10,2,0,0" />
            </StackPanel>
        </Border>

        <!-- Summary Cards + Alerts -->
        <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto">
            <StackPanel>
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="12" />
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="12" />
                        <ColumnDefinition Width="*" />
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="12" />
                        <RowDefinition Height="Auto" />
                    </Grid.RowDefinitions>

                    <!-- Card 1: Total Students -->
                    <Border Grid.Row="0" Grid.Column="0" Background="#EFF6FF" BorderBrush="#BFDBFE" BorderThickness="1" CornerRadius="8" Padding="16">
                        <StackPanel>
                            <TextBlock Text="👨‍🎓 مجموع شاگردان" FontSize="14" FontWeight="SemiBold" Foreground="#1E40AF" />
                            <TextBlock x:Name="TotalStudentsTextBlock" Text="۰" FontSize="28" FontWeight="Bold" Foreground="#1E3A8A" Margin="0,8,0,0" />
                        </StackPanel>
                    </Border>

                    <!-- Card 2: Total Classes -->
                    <Border Grid.Row="0" Grid.Column="2" Background="#ECFDF5" BorderBrush="#A7F3D0" BorderThickness="1" CornerRadius="8" Padding="16">
                        <StackPanel>
                            <TextBlock Text="🏫 مجموع صنف‌ها" FontSize="14" FontWeight="SemiBold" Foreground="#047857" />
                            <TextBlock x:Name="TotalClassesTextBlock" Text="۰" FontSize="28" FontWeight="Bold" Foreground="#064E3B" Margin="0,8,0,0" />
                        </StackPanel>
                    </Border>

                    <!-- Card 3: Today's Attendance -->
                    <Border Grid.Row="0" Grid.Column="4" Background="#FEF3C7" BorderBrush="#FDE68A" BorderThickness="1" CornerRadius="8" Padding="16">
                        <StackPanel>
                            <TextBlock Text="🗓️ حاضری امروز" FontSize="14" FontWeight="SemiBold" Foreground="#B45309" />
                            <TextBlock x:Name="TodayAttendanceTextBlock" Text="۰/۰" FontSize="28" FontWeight="Bold" Foreground="#92400E" Margin="0,8,0,0" />
                            <TextBlock x:Name="TodayAbsenceRateTextBlock" Text="غیبت: ۰%" FontSize="12" Foreground="#92400E" Margin="0,4,0,0" />
                        </StackPanel>
                    </Border>

                    <!-- Card 4: Outstanding Fees -->
                    <Border Grid.Row="2" Grid.Column="0" Background="#FEE2E2" BorderBrush="#FECACA" BorderThickness="1" CornerRadius="8" Padding="16">
                        <StackPanel>
                            <TextBlock Text="💰 فیس باقی‌مانده" FontSize="14" FontWeight="SemiBold" Foreground="#B91C1C" />
                            <TextBlock x:Name="OutstandingFeesTextBlock" Text="۰" FontSize="28" FontWeight="Bold" Foreground="#991B1B" Margin="0,8,0,0" />
                        </StackPanel>
                    </Border>

                    <!-- Card 5: Overdue Books -->
                    <Border Grid.Row="2" Grid.Column="2" Background="#F3E8FF" BorderBrush="#D8B4FE" BorderThickness="1" CornerRadius="8" Padding="16">
                        <StackPanel>
                            <TextBlock Text="📚 کتاب‌های عقب‌مانده" FontSize="14" FontWeight="SemiBold" Foreground="#7E22CE" />
                            <TextBlock x:Name="OverdueBooksTextBlock" Text="۰" FontSize="28" FontWeight="Bold" Foreground="#6B21A8" Margin="0,8,0,0" />
                        </StackPanel>
                    </Border>

                    <!-- Card 6: Recent Audit Logs (Admin only) -->
                    <Border Grid.Row="2" Grid.Column="4" Background="#F1F5F9" BorderBrush="#CBD5E1" BorderThickness="1" CornerRadius="8" Padding="16">
                        <StackPanel>
                            <TextBlock Text="📋 آخرین وقایع" FontSize="14" FontWeight="SemiBold" Foreground="#334155" />
                            <TextBlock x:Name="RecentAuditTextBlock" Text="" FontSize="12" Foreground="#475569" Margin="0,8,0,0" TextWrapping="Wrap" MaxHeight="80" />
                        </StackPanel>
                    </Border>
                </Grid>

                <!-- Alerts Card -->
                <Border Background="#FFFBEB" BorderBrush="#FDE68A" BorderThickness="1" CornerRadius="8" Padding="16" Margin="0,12,0,0">
                    <StackPanel>
                        <TextBlock Text="🔔 اعلان‌ها" FontSize="14" FontWeight="SemiBold" Foreground="#B45309" />
                        <TextBlock x:Name="AlertsCountTextBlock" Text="۰ اعلان" FontSize="16" FontWeight="Bold" Foreground="#92400E" Margin="0,8,0,0" />
                        <ItemsControl x:Name="AlertsListItemsControl" Margin="0,8,0,0">
                            <ItemsControl.ItemTemplate>
                                <DataTemplate>
                                    <TextBlock Text="{Binding Message}" FontSize="12" Foreground="#78350F" TextWrapping="Wrap" />
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>
                    </StackPanel>
                </Border>
            </StackPanel>
        </ScrollViewer>
    </Grid>
</UserControl>

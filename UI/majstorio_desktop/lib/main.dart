import 'package:flutter/material.dart';
import 'package:majstorio_desktop/providers/appointment/appointment_provider.dart';
import 'package:majstorio_desktop/providers/category/category_provider.dart';
import 'package:majstorio_desktop/providers/dashboard/dashboard_provider.dart';
import 'package:majstorio_desktop/providers/offer/offer_provider.dart';
import 'package:majstorio_desktop/providers/report/report_provider.dart';
import 'package:majstorio_desktop/providers/review/review_provider.dart';
import 'package:majstorio_desktop/providers/service_request/service_request_provider.dart';
import 'package:majstorio_desktop/providers/tradesperson_profile/tradesperson_profile_provider.dart';
import 'package:majstorio_desktop/providers/tradesperson_service/tradesperson_service_provider.dart';
import 'package:majstorio_desktop/providers/payment/payment_provider.dart';
import 'package:majstorio_desktop/providers/city/city_provider.dart';
import 'package:majstorio_desktop/providers/news_post/news_provider.dart';
import 'package:majstorio_desktop/providers/user/user_provider.dart';
import 'package:majstorio_desktop/constants.dart';
import 'package:majstorio_desktop/providers/auth/auth_provider.dart';
import 'package:majstorio_desktop/screens/login_screen.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';
import 'package:provider/provider.dart';

void main() {
  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (ctx) => AuthProvider()),
        ChangeNotifierProvider(create: (ctx) => CityProvider()),
        ChangeNotifierProvider(create: (ctx) => CategoryProvider()),
        ChangeNotifierProvider(create: (ctx) => NewsProvider()),
        ChangeNotifierProvider(create: (ctx) => UserProvider()),
        ChangeNotifierProvider(create: (ctx) => ReviewProvider()),
        ChangeNotifierProvider(create: (ctx) => OfferProvider()),
        ChangeNotifierProvider(create: (ctx) => AppointmentProvider()),
        ChangeNotifierProvider(create: (ctx) => DashboardProvider()),
        ChangeNotifierProvider(create: (ctx) => ReportProvider()),
        ChangeNotifierProvider(create: (ctx) => ServiceRequestProvider()),
        ChangeNotifierProvider(create: (ctx) => TradespersonProfileProvider()),
        ChangeNotifierProvider(create: (ctx) => TradespersonServiceProvider()),
        ChangeNotifierProvider(create: (ctx) => PaymentProvider()),
      ],
    child: MyApp(),),
  );
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Majstorio Admin',
      theme: AppTheme.light,
      // The providers use this key to return to the login screen when the API answers 401.
      navigatorKey: navigatorKey,
      // Nothing is available before signing in, so the app starts on the login screen.
      home: const LoginScreen(),
      routes: {loginRoute: (context) => const LoginScreen()},
    );
  }
}

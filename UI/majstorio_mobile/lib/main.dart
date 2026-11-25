import 'package:flutter/material.dart';
import 'package:majstorio_mobile/constants.dart';
import 'package:majstorio_mobile/providers/account/account_provider.dart';
import 'package:majstorio_mobile/providers/appointment/appointment_provider.dart';
import 'package:majstorio_mobile/providers/auth/auth_provider.dart';
import 'package:majstorio_mobile/providers/category/category_provider.dart';
import 'package:majstorio_mobile/providers/city/city_provider.dart';
import 'package:majstorio_mobile/providers/news_post/news_provider.dart';
import 'package:majstorio_mobile/providers/notification/notification_provider.dart';
import 'package:majstorio_mobile/providers/offer/offer_provider.dart';
import 'package:majstorio_mobile/providers/review/review_provider.dart';
import 'package:majstorio_mobile/providers/service_request/service_request_provider.dart';
import 'package:majstorio_mobile/providers/tradesperson_profile/tradesperson_profile_provider.dart';
import 'package:majstorio_mobile/screens/login_screen.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:provider/provider.dart';

void main() {
  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (ctx) => AuthProvider()),
        ChangeNotifierProvider(create: (ctx) => CityProvider()),
        ChangeNotifierProvider(create: (ctx) => CategoryProvider()),
        ChangeNotifierProvider(create: (ctx) => NewsProvider()),
        ChangeNotifierProvider(create: (ctx) => TradespersonProfileProvider()),
        ChangeNotifierProvider(create: (ctx) => ServiceRequestProvider()),
        ChangeNotifierProvider(create: (ctx) => OfferProvider()),
        ChangeNotifierProvider(create: (ctx) => AppointmentProvider()),
        ChangeNotifierProvider(create: (ctx) => ReviewProvider()),
        ChangeNotifierProvider(create: (ctx) => AccountProvider()),
        ChangeNotifierProvider(create: (ctx) => NotificationProvider()),
      ],
      child: const MyApp(),
    ),
  );
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Majstorio',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light,
      // The providers use this key to return to the login screen when the API answers 401.
      navigatorKey: navigatorKey,
      // Nothing is available before signing in, so the app starts on the login screen.
      home: const LoginScreen(),
      routes: {loginRoute: (context) => const LoginScreen()},
    );
  }
}

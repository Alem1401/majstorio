import 'package:flutter/material.dart';
import 'package:majstorio_desktop/providers/appointment/appointment_provider.dart';
import 'package:majstorio_desktop/providers/category/category_provider.dart';
import 'package:majstorio_desktop/providers/offer/offer_provider.dart';
import 'package:majstorio_desktop/providers/review/review_provider.dart';
import 'package:majstorio_desktop/providers/city/city_provider.dart';
import 'package:majstorio_desktop/providers/news_post/news_provider.dart';
import 'package:majstorio_desktop/providers/user/user_provider.dart';
import 'package:majstorio_desktop/screens/dashboard_screen.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';
import 'package:provider/provider.dart';

void main() {
  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (ctx) => CityProvider()),
        ChangeNotifierProvider(create: (ctx) => CategoryProvider()),
        ChangeNotifierProvider(create: (ctx) => NewsProvider()),
        ChangeNotifierProvider(create: (ctx) => UserProvider()),
        ChangeNotifierProvider(create: (ctx) => ReviewProvider()),
        ChangeNotifierProvider(create: (ctx) => OfferProvider()),
        ChangeNotifierProvider(create: (ctx) => AppointmentProvider()),
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
      home: const MyWidget(),
    );
  }
}

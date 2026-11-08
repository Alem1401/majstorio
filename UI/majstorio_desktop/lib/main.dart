import 'package:flutter/material.dart';
import 'package:majstorio_desktop/providers/category_provider.dart';
import 'package:majstorio_desktop/providers/city_provider.dart';
import 'package:majstorio_desktop/screens/dashboard_screen.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';
import 'package:provider/provider.dart';

void main() {
  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (ctx) => CityProvider()),
        ChangeNotifierProvider(create: (ctx) => CategoryProvider()),
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

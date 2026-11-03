import 'package:flutter/material.dart';
import 'package:majstorio_desktop/screens/dashboard_screen.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';

void main() {
  runApp(const MyApp());
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

import 'package:flutter/material.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';

// The frame shared by every screen: top bar with the gold line under it, and the content of the screen.
// Same as the desktop MasterScreen; the menu lives in the bottom navigation (MainNavigation) instead of a drawer.
class MasterScreen extends StatelessWidget {
  const MasterScreen({
    super.key,
    required this.title,
    required this.child,
    this.actions,
    this.floatingActionButton,
  });

  final String title;
  final Widget child;
  final List<Widget>? actions;
  final Widget? floatingActionButton;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(title),
        actions: actions,
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(2),
          child: Container(height: 2, color: AppTheme.gold),
        ),
      ),
      floatingActionButton: floatingActionButton,
      body: SafeArea(top: false, child: child),
    );
  }
}

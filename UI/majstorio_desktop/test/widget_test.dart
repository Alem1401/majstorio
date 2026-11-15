import 'package:flutter_test/flutter_test.dart';
import 'package:majstorio_desktop/main.dart';
import 'package:majstorio_desktop/providers/dashboard/dashboard_provider.dart';
import 'package:provider/provider.dart';

void main() {
  testWidgets('the desktop home screen renders', (tester) async {
    // Tests have no API, so the dashboard shows its "could not be loaded" message instead of numbers.
    await tester.pumpWidget(
      ChangeNotifierProvider(
        create: (context) => DashboardProvider(),
        child: const MyApp(),
      ),
    );
    await tester.pumpAndSettle();

    expect(tester.takeException(), null);
    expect(find.text('Welcome, Administrator'), findsOneWidget);
    expect(find.textContaining('could not be loaded'), findsOneWidget);
  });
}

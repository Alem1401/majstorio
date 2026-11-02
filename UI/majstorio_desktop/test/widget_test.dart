import 'package:flutter_test/flutter_test.dart';
import 'package:majstorio_desktop/main.dart';

void main() {
  testWidgets('the desktop home screen renders', (tester) async {
    await tester.pumpWidget(const MyApp());

    expect(tester.takeException(), null);
    expect(find.text('Name'), findsWidgets);
  });
}

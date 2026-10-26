namespace PayAhead.Application.Localization;

public static class NotificationText
{
    private static readonly IReadOnlyDictionary<string, (string Payment, string Trial)> Text = new Dictionary<string, (string, string)>
    {
        ["tr"] = ("Bir ödeme hatırlatmanız var.", "Bir deneme bitişi hatırlatmanız var."),
        ["en"] = ("You have a payment reminder.", "You have a trial ending reminder."),
        ["de"] = ("Sie haben eine Zahlungserinnerung.", "Sie haben eine Erinnerung an das Ende einer Testphase."),
        ["fr"] = ("Vous avez un rappel de paiement.", "Vous avez un rappel de fin d’essai."),
        ["es"] = ("Tienes un recordatorio de pago.", "Tienes un recordatorio del fin de una prueba."),
        ["it"] = ("Hai un promemoria di pagamento.", "Hai un promemoria di fine prova."),
        ["pt-BR"] = ("Você tem um lembrete de pagamento.", "Você tem um lembrete de fim do período de teste."),
        ["nl"] = ("Je hebt een betalingsherinnering.", "Je hebt een herinnering voor het einde van een proefperiode."),
        ["ru"] = ("У вас напоминание о платеже.", "У вас напоминание об окончании пробного периода."),
        ["ar"] = ("لديك تذكير بالدفع.", "لديك تذكير بانتهاء الفترة التجريبية."),
        ["zh-Hans"] = ("您有一条付款提醒。", "您有一条试用期结束提醒。"),
        ["ja"] = ("お支払いのリマインダーがあります。", "無料体験終了のリマインダーがあります。"),
        ["ko"] = ("결제 알림이 있습니다.", "체험 기간 종료 알림이 있습니다."),
        ["hi"] = ("आपके लिए भुगतान का रिमाइंडर है।", "आपके लिए परीक्षण अवधि समाप्त होने का रिमाइंडर है।")
    };
    public static string Body(string? language, bool trial)
    {
        var value = Text[SupportedLanguages.Resolve(language)]; return trial ? value.Trial : value.Payment;
    }
}

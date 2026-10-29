// Yalnız otomatik smoke hesapları için açık test teyitleri; uygulama bu modülü kullanmaz.
exports.registrationLegal = () => {
  const version = '2026-10-29-draft-1';
  return { legalVersion: version, legal: { noticeVersion: version, noticeAcknowledged: true, privacyVersion: version, privacyAcknowledged: true, termsVersion: version, termsAccepted: true } };
};

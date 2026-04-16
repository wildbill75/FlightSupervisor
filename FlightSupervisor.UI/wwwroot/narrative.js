/**
 * Flight Supervisor - Chief Pilot Narrative Debrief Generator
 * Analyzes the final flight report metrics and generates a synthetic, serious, and detailed textual debriefing.
 */

window.generateChiefPilotDebrief = function(rep, lang = 'en') {
    let p1 = ""; // Overall Assessment
    let p2 = ""; // Category Focus
    let p3 = ""; // Tech/Landing specifics & conclusion

    const isFr = lang === 'fr';

    // 1. Overall Assessment
    if (rep.Score >= 1100) {
        p1 = isFr 
            ? `Bienvenue à la base, Capitaine. J'ai examiné vos relevés de vol, et c'est tout simplement un cas d'école. Les performances globales sont excellentes.` 
            : `Welcome back to base, Captain. I've reviewed your flight logs, and it's a textbook example of great airmanship. Overall performance is outstanding.`;
    } else if (rep.Score >= 900) {
        p1 = isFr
            ? `Bon retour, Capitaine. Les opérations se sont déroulées globalement comme prévu, c'est un vol solide, bien qu'il y ait des pistes d'amélioration mineures.`
            : `Welcome back, Captain. Operations proceeded mostly as expected today. A solid flight overall, though there is always room for minor improvements.`;
    } else if (rep.Score >= 500) {
        p1 = isFr
            ? `Capitaine, nous avons un bilan très mitigé sur ce vol. Les standards de la compagnie ont à peine été respectés. Il va falloir analyser ces erreurs.`
            : `Captain, we have a very mixed debriefing for this flight. The airline's standards were barely met. We need to analyze what went wrong.`;
    } else {
        p1 = isFr
            ? `Ce compte-rendu est inacceptable, Capitaine. La gestion de ce vol pose de sérieux problèmes pour la compagnie. Vous êtes convoqué dans mon bureau immédiatement.`
            : `This log is unacceptable, Captain. The management of this flight raises serious concerns for the airline. You are expected in my office immediately.`;
    }

    // Punctuality addition to P1
    if (rep.DelaySec > 900) {
        p1 += isFr 
            ? ` De plus, accumuler autant de retard (${Math.round(rep.DelaySec/60)} minutes) désorganise fortement notre réseau.`
            : ` Furthermore, accumulating a delay of ${Math.round(rep.DelaySec/60)} minutes heavily disrupts our network schedule.`;
    } else if (rep.DelaySec <= 300 && rep.RawDelaySec > 300) {
        // Delayed block off but managed to catch up in flight
        p1 += isFr
            ? ` Je note toutefois un bel effort pour avoir rattrapé le retard accumulé au sol durant le vol.`
            : ` I do commend your successful effort in making up for the ground delay during the flight.`;
    } else if (rep.RawDelaySec < -300) {
        p1 += isFr
            ? ` Vos opérations ont même été en avance sur le bloc horaire prévu, excellent travail.`
            : ` Your operations even ran ahead of the scheduled block time, excellent work.`;
    } else {
        p1 += isFr
            ? ` La ponctualité a par ailleurs été parfaitement tenue.`
            : ` Schedule integrity was perfectly maintained.`;
    }

    // 2. Category Focus (Analyze Safety vs Comfort vs Ops)
    // Assume base scores: Safety (500), Comfort (300), Ops (200), Maint (0 base).
    // Let's see what they lost.
    let lostSafety = 500 - ((rep.AirmanshipPoints ?? 0) + (rep.AbnormalOperationsPoints ?? 0));
    let lostComfort = 300 - (rep.PassengerExperiencePoints ?? rep.ComfortPoints ?? 0);
    let lostOps = 200 - ((rep.FlightPhaseFlowsPoints ?? 0) + (rep.CommunicationPoints ?? 0));

    if (lostSafety > 100) {
        p2 = isFr
            ? `Cependant, le plus inquiétant reste la sécurité. Les logs montrent de multiples infractions aux limitations ou au domaine de vol. La sécurité prime sur tout le reste.`
            : `The most alarming aspect is safety. Your logs show multiple violations of procedures or flight envelope limits. Safety must always be your top priority.`;
    } else if (lostComfort > 80) {
        p2 = isFr
            ? `Si le vol était sûr, le confort passager a été largement négligé. Les G-Forces, virages brusques ou phases non communiquées ont généré de nombreuses plaintes en cabine.`
            : `While the flight was safe, passenger comfort was largely neglected. Sharp maneuvers, G-forces, or poor cabin communication generated numerous complaints.`;
    } else if (lostOps > 50) {
        p2 = isFr
            ? `Vous devez cependant faire un effort sur la gestion des opérations au sol et des flux de communication avec la piste ou la cabine pour fluidifier l'expérience.`
            : `You need to put more effort into managing ground operations and communication flows with the ramp and cabin crew to smooth the experience.`;
    } else if (rep.Score >= 1000) {
        p2 = isFr
            ? `La gestion des impondérables au sol et l'anticipation du confort cabine ont été irréprochables. Chaque décision a été prise dans l'intérêt de la compagnie.`
            : `The management of ground imponderables and your anticipation of cabin comfort were flawless. Every decision was made in the airline's best interest.`;
    } else {
        p2 = isFr
            ? `Il y a eu quelques perturbations causées par vos choix tactiques ou opérationnels, mais rien hors de contrôle pour l'entreprise.`
            : `There were a few disruptions caused by your tactical or operational choices, but nothing out of control for the company.`;
    }

    // Objectives addition
    if (rep.Objectives && rep.Objectives.length > 0) {
        let failedObjs = rep.Objectives.filter(o => !o.Passed).length;
        if (failedObjs === 0) {
            p2 += isFr 
                ? ` Je suis très satisfait de voir que tous les objectifs commerciaux ciblés pour cette route ont été remplis.`
                : ` I am highly satisfied to see you cleared all the targeted commercial objectives for this route.`;
        } else {
            p2 += isFr
                ? ` Notons tout de même que ${failedObjs} objectif(s) opérationnel(s) contractuel(s) n'a/ont pas pu être tenu(s).`
                : ` Do note that ${failedObjs} operational objective(s) set by the company were failed.`;
        }
    }

    // 3. Tech/Landing Specifics & Conclusion
    if (rep.TouchdownFpm < -500) {
        p3 = isFr
            ? `Pour finir, un mot sur cet atterrissage à ${Math.round(rep.TouchdownFpm)} fpm enregistrant ${rep.TouchdownGForce.toFixed(2)} G. C'était bien au-delà de l'acceptable. Nos mécaniciens doivent maintenant inspecter le train, un rapport d'anomalie a été ouvert. Reposez-vous.`
            : `Lastly, a word on that ${Math.round(rep.TouchdownFpm)} fpm touchdown registering ${rep.TouchdownGForce.toFixed(2)} Gs. That was well beyond acceptable limits. Our mechanics must now inspect the landing gear, and a hard-landing report has been filed. Get some rest.`;
    } else if (rep.TouchdownFpm < -350 || rep.TouchdownGForce >= 1.4) {
        p3 = isFr
            ? `L'atterrissage final (${Math.round(rep.TouchdownFpm)} fpm, ${rep.TouchdownGForce.toFixed(2)} G) était particulièrement ferme. Tant que c'est dans la zone de toucher des roues, c'est sûr, mais essayez de préserver nos amortisseurs et les vertèbres de nos clients à l'avenir.`
            : `That final touchdown (${Math.round(rep.TouchdownFpm)} fpm, ${rep.TouchdownGForce.toFixed(2)} G) was noticeably firm. As long as it's in the touchdown zone, it's safe, but please try to spare our struts and our customers' spines in the future.`;
    } else if (rep.TouchdownFpm > -150) {
        p3 = isFr
            ? `Superbe kiss-landing final relevé à ${Math.round(rep.TouchdownFpm)} fpm. Les passagers n'ont même pas senti le contact. Terminez vos formalités administratives et à demain.`
            : `A textbook "kiss landing" recorded at ${Math.round(rep.TouchdownFpm)} fpm. The passengers barely felt the asphalt. Finish your paperwork and we'll see you tomorrow.`;
    } else {
        p3 = isFr
            ? `Atterrissage parfaitement standard enregistré à ${Math.round(rep.TouchdownFpm)} fpm, sécurité maximale. Merci pour ce vol, et terminez votre block de papier. Rompez.`
            : `A perfectly standard touchdown recorded at ${Math.round(rep.TouchdownFpm)} fpm, maintaining maximum safety. Thank you for this flight, please wrap up your paperwork. Dismissed.`;
    }

    return `
        <p class="mb-3 text-slate-300">${p1}</p>
        <p class="mb-3 text-slate-300">${p2}</p>
        <p class="text-slate-300">${p3}</p>
    `;
};
